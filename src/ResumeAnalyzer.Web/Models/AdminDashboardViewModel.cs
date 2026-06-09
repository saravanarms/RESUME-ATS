using System;
using System.Collections.Generic;

namespace ResumeAnalyzer.Web.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalResumes { get; set; }
        public int AverageAtsScore { get; set; }
        public decimal MonthlyRevenue { get; set; }
        
        public int FreeUsersCount { get; set; }
        public int ProUsersCount { get; set; }
        public int EnterpriseUsersCount { get; set; }
        
        public List<AdminUserItemViewModel> Users { get; set; } = new();
    }

    public class AdminUserItemViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string SubscriptionTier { get; set; } = string.Empty;
        public int UsageThisMonth { get; set; }
        public DateTime DateRegistered { get; set; }
    }
}
