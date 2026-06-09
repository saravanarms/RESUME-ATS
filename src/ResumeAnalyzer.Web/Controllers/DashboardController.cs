using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResumeAnalyzer.Domain.Entities;
using ResumeAnalyzer.Infrastructure.Data;
using ResumeAnalyzer.Web.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ResumeAnalyzer.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var resumes = await _context.Resumes
                .Where(r => r.UserId == userId && !r.IsDeleted)
                .Include(r => r.AnalysisResults)
                .ToListAsync();

            var totalResumes = resumes.Count;
            
            // Calculate Average ATS score from all completed analyses
            var allResults = resumes.SelectMany(r => r.AnalysisResults).ToList();
            var avgAtsScore = allResults.Any() ? (int)allResults.Average(ar => ar.AtsScore) : 0;
            
            var recentAnalyses = allResults
                .OrderByDescending(ar => ar.AnalyzedAt)
                .Take(5)
                .Select(ar => new RecentAnalysisViewModel
                {
                    AnalysisId = ar.Id,
                    ResumeName = resumes.First(r => r.Id == ar.ResumeId).FileName,
                    JobTitle = ar.JobTitle ?? "General Match",
                    AtsScore = ar.AtsScore,
                    DateAnalyzed = ar.AnalyzedAt
                })
                .ToList();

            // Prepare Trend Data for Chart
            var trendData = allResults
                .OrderBy(ar => ar.AnalyzedAt)
                .Take(10)
                .Select(ar => new AtsTrendPoint
                {
                    DateLabel = ar.AnalyzedAt.ToString("MMM dd"),
                    Score = ar.AtsScore
                })
                .ToList();

            var dashboardVm = new DashboardViewModel
            {
                TotalResumes = totalResumes,
                AverageAtsScore = avgAtsScore,
                RecentAnalyses = recentAnalyses,
                TrendPoints = trendData
            };

            return View(dashboardVm);
        }
    }
}
