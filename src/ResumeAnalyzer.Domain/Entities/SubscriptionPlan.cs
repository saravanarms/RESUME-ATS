using System;
using System.Collections.Generic;

namespace ResumeAnalyzer.Domain.Entities
{
    public class SubscriptionPlan
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int MaxResumesPerMonth { get; set; }
        public string? FeaturesJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}
