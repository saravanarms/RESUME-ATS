using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ResumeAnalyzer.Domain.Entities;

namespace ResumeAnalyzer.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Resume> Resumes => Set<Resume>();
        public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();
        public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser & SubscriptionPlan relationship
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.SubscriptionPlan)
                .WithMany(p => p.Users)
                .HasForeignKey(u => u.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Resume & ApplicationUser relationship
            builder.Entity<Resume>()
                .HasOne(r => r.User)
                .WithMany(u => u.Resumes)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure AnalysisResult & Resume relationship
            builder.Entity<AnalysisResult>()
                .HasOne(a => a.Resume)
                .WithMany(r => r.AnalysisResults)
                .HasForeignKey(a => a.ResumeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure decimal precision for SubscriptionPlan Price
            builder.Entity<SubscriptionPlan>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            // Seed default plans
            builder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan
                {
                    Id = 1,
                    Name = "Free",
                    Price = 0.00m,
                    MaxResumesPerMonth = 3,
                    FeaturesJson = "[\"3 Resumes/month\", \"Standard ATS Scoring\", \"Basic Recommendations\"]"
                },
                new SubscriptionPlan
                {
                    Id = 2,
                    Name = "Pro",
                    Price = 19.99m,
                    MaxResumesPerMonth = 50,
                    FeaturesJson = "[\"50 Resumes/month\", \"Advanced ATS Matching\", \"Detailed Skill Gap Analysis\", \"PDF Report Downloads\", \"Priority AI Queue\"]"
                },
                new SubscriptionPlan
                {
                    Id = 3,
                    Name = "Enterprise",
                    Price = 99.00m,
                    MaxResumesPerMonth = 500,
                    FeaturesJson = "[\"500 Resumes/month\", \"Full Dashboard Analytics\", \"Custom Skill Mapping\", \"Unlimited PDF Downloads\", \"Dedicated API Key\", \"Account Manager\"]"
                }
            );
        }
    }
}
