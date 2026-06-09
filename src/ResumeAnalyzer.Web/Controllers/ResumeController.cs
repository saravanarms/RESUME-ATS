using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResumeAnalyzer.Domain.Entities;
using ResumeAnalyzer.Domain.Interfaces;
using ResumeAnalyzer.Infrastructure.Data;
using ResumeAnalyzer.Web.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ResumeAnalyzer.Web.Controllers
{
    [Authorize]
    public class ResumeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAzureBlobStorageService _blobService;
        private readonly IFastAPIClient _fastApiClient;

        public ResumeController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IAzureBlobStorageService blobService,
            IFastAPIClient fastApiClient)
        {
            _context = context;
            _userManager = userManager;
            _blobService = blobService;
            _fastApiClient = fastApiClient;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, string jobDescription, string? jobTitle)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Please upload a valid PDF or DOCX file.");
                return View();
            }

            if (string.IsNullOrEmpty(jobDescription))
            {
                ModelState.AddModelError("jobDescription", "Please provide a job description for matching.");
                return View();
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".pdf" && extension != ".docx")
            {
                ModelState.AddModelError("file", "Only PDF and DOCX files are allowed.");
                return View();
            }

            var userId = _userManager.GetUserId(User);
            var user = await _context.Users
                .Include(u => u.SubscriptionPlan)
                .FirstOrDefaultAsync(u => u.Id == userId);
                
            if (user == null) return Challenge();

            // Validate usage limits based on subscription
            var maxResumes = user.SubscriptionPlan?.MaxResumesPerMonth ?? 3;
            if (user.UsageCountThisMonth >= maxResumes && user.SubscriptionPlanId == 1) // strictly enforce for free tier
            {
                TempData["ErrorMessage"] = "You have reached your monthly limit of resume analyses. Please upgrade your plan for more limits.";
                return RedirectToAction("Index", "Dashboard");
            }

            try
            {
                // 1. Upload to Blob Storage / Local fallback
                string blobUrl;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    blobUrl = await _blobService.UploadFileAsync(memoryStream, file.FileName, file.ContentType);
                }

                // 2. Save Resume Entity
                var resume = new Resume
                {
                    UserId = userId,
                    FileName = file.FileName,
                    BlobUrl = blobUrl,
                    UploadedAt = DateTime.UtcNow
                };
                _context.Resumes.Add(resume);
                await _context.SaveChangesAsync();

                // 3. Call FastAPI Backend for Analysis
                var token = HttpContext.Session.GetString("JwtToken") ?? "";
                
                using var fileStreamForApi = file.OpenReadStream();
                var rawResultJson = await _fastApiClient.CompareJobDescriptionAsync(
                    fileStreamForApi, 
                    file.FileName, 
                    null, 
                    jobDescription, 
                    token
                );

                // 4. Parse response & Save AnalysisResult
                using var doc = JsonDocument.Parse(rawResultJson);
                var root = doc.RootElement;

                var atsScore = root.GetProperty("ats_score").GetInt32();
                var keywordMatch = root.GetProperty("keyword_match_percentage").GetInt32();
                var missingSkills = root.GetProperty("missing_skills").ToString();
                var recommendations = root.GetProperty("recommendations").ToString();
                var formattingIssues = root.GetProperty("formatting_issues").ToString();
                var grammarSuggestions = root.GetProperty("grammar_suggestions").ToString();
                var summary = root.TryGetProperty("resume_summary", out var sumProp) ? sumProp.GetString() : "";

                var analysisResult = new AnalysisResult
                {
                    ResumeId = resume.Id,
                    JobTitle = jobTitle ?? "General Match",
                    JobDescription = jobDescription,
                    AtsScore = atsScore,
                    KeywordMatchPercentage = keywordMatch,
                    MissingSkillsJson = missingSkills,
                    RecommendationsJson = recommendations,
                    FormattingIssuesJson = formattingIssues,
                    GrammarSuggestionsJson = grammarSuggestions,
                    ResumeSummary = summary,
                    AnalyzedAt = DateTime.UtcNow
                };

                _context.AnalysisResults.Add(analysisResult);
                
                // Update User limits
                user.UsageCountThisMonth += 1;
                user.LastUsageDate = DateTime.UtcNow;
                _context.Users.Update(user);

                await _context.SaveChangesAsync();

                return RedirectToAction("Result", new { id = analysisResult.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to analyze resume: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Result(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var result = await _context.AnalysisResults
                .Include(ar => ar.Resume)
                .FirstOrDefaultAsync(ar => ar.Id == id && ar.Resume!.UserId == userId);

            if (result == null)
            {
                return NotFound();
            }

            // Deserialize JSON arrays for view model
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var missingSkills = JsonSerializer.Deserialize<string[]>(result.MissingSkillsJson, options) ?? Array.Empty<string>();
            var recommendations = JsonSerializer.Deserialize<string[]>(result.RecommendationsJson, options) ?? Array.Empty<string>();
            var formattingIssues = JsonSerializer.Deserialize<string[]>(result.FormattingIssuesJson, options) ?? Array.Empty<string>();
            var grammarSuggestions = JsonSerializer.Deserialize<string[]>(result.GrammarSuggestionsJson, options) ?? Array.Empty<string>();

            var vm = new AnalysisResultViewModel
            {
                AnalysisId = result.Id,
                FileName = result.Resume?.FileName ?? "Resume",
                BlobUrl = result.Resume?.BlobUrl ?? string.Empty,
                JobTitle = result.JobTitle ?? "Job Description",
                JobDescription = result.JobDescription,
                AtsScore = result.AtsScore,
                KeywordMatchPercentage = result.KeywordMatchPercentage,
                MissingSkills = missingSkills,
                Recommendations = recommendations,
                FormattingIssues = formattingIssues,
                GrammarSuggestions = grammarSuggestions,
                ResumeSummary = result.ResumeSummary ?? "No summary provided.",
                AnalyzedAt = result.AnalyzedAt
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var userId = _userManager.GetUserId(User);
            var results = await _context.AnalysisResults
                .Include(ar => ar.Resume)
                .Where(ar => ar.Resume!.UserId == userId && !ar.Resume.IsDeleted)
                .OrderByDescending(ar => ar.AnalyzedAt)
                .Select(ar => new HistoryItemViewModel
                {
                    AnalysisId = ar.Id,
                    ResumeName = ar.Resume!.FileName,
                    JobTitle = ar.JobTitle ?? "General Match",
                    AtsScore = ar.AtsScore,
                    DateAnalyzed = ar.AnalyzedAt,
                    BlobUrl = ar.Resume.BlobUrl
                })
                .ToListAsync();

            return View(results);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var result = await _context.AnalysisResults
                .Include(ar => ar.Resume)
                .FirstOrDefaultAsync(ar => ar.Id == id && ar.Resume!.UserId == userId);

            if (result != null)
            {
                // Soft delete or hard delete resume
                result.Resume!.IsDeleted = true;
                _context.Resumes.Update(result.Resume);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("History");
        }
    }
}
