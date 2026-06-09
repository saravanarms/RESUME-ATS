using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace ResumeAnalyzer.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        
        public int SubscriptionPlanId { get; set; } = 1; // Default to Free plan (Id = 1)
        public string SubscriptionStatus { get; set; } = "Active";
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }

        public int UsageCountThisMonth { get; set; } = 0;
        public DateTime? LastUsageDate { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public SubscriptionPlan? SubscriptionPlan { get; set; }
        public ICollection<Resume> Resumes { get; set; } = new List<Resume>();
    }
}
