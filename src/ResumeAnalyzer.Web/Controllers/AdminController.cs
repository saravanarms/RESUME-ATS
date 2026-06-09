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
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Gather statistics
            var totalUsers = await _context.Users.CountAsync();
            var totalResumes = await _context.Resumes.CountAsync();
            var averageAtsScore = await _context.AnalysisResults.AnyAsync()
                ? (int)await _context.AnalysisResults.AverageAsync(ar => ar.AtsScore)
                : 0;

            // Fetch User Subscription metrics
            var proUsersCount = await _context.Users.CountAsync(u => u.SubscriptionPlanId == 2);
            var enterpriseUsersCount = await _context.Users.CountAsync(u => u.SubscriptionPlanId == 3);
            var freeUsersCount = totalUsers - proUsersCount - enterpriseUsersCount;

            // Est. monthly revenue based on subscription prices
            var monthlyRevenue = (proUsersCount * 19.99m) + (enterpriseUsersCount * 99.00m);

            // Fetch users list
            var users = await _context.Users
                .Include(u => u.SubscriptionPlan)
                .OrderByDescending(u => u.DateCreated)
                .Take(20)
                .Select(u => new AdminUserItemViewModel
                {
                    UserId = u.Id,
                    Email = u.Email ?? "",
                    FullName = u.FullName ?? "N/A",
                    SubscriptionTier = u.SubscriptionPlan != null ? u.SubscriptionPlan.Name : "Free",
                    UsageThisMonth = u.UsageCountThisMonth,
                    DateRegistered = u.DateCreated
                })
                .ToListAsync();

            var adminVm = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalResumes = totalResumes,
                AverageAtsScore = averageAtsScore,
                MonthlyRevenue = monthlyRevenue,
                FreeUsersCount = freeUsersCount,
                ProUsersCount = proUsersCount,
                EnterpriseUsersCount = enterpriseUsersCount,
                Users = users
            };

            return View(adminVm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSubscription(string userId, int planId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.SubscriptionPlanId = planId;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction("Index");
        }
    }
}
