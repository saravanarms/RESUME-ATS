using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using ResumeAnalyzer.Domain.Entities;
using ResumeAnalyzer.Domain.Interfaces;
using ResumeAnalyzer.Infrastructure.Data;
using ResumeAnalyzer.Infrastructure.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ResumeAnalyzer.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Add DB Context
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                ?? "Data Source=ResumeAnalyzer.db";
            
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString, b => b.MigrationsAssembly("ResumeAnalyzer.Infrastructure")));

            // 2. Add ASP.NET Core Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Configure Authentication cookies
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/Login";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
            });

            // 3. Add Session support (to store JWT token for FastAPI)
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddControllersWithViews();

            // 4. Add Storage Service
            builder.Services.AddSingleton<IAzureBlobStorageService, AzureBlobStorageService>();

            // 5. Register Typed HttpClient for FastAPI backend with Polly Resiliency
            builder.Services.AddHttpClient<IFastAPIClient, FastAPIClient>()
                .AddPolicyHandler(GetRetryPolicy());

            var app = builder.Build();

            // Configure HTTP pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession(); // Required before authentication/authorization

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Seed DB Roles and Admin user
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    // Run database migrations if deploying to cloud
                    await context.Database.EnsureCreatedAsync();

                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

                    // Seed Roles
                    string[] roleNames = { "Admin", "Candidate" };
                    foreach (var roleName in roleNames)
                    {
                        if (!await roleManager.RoleExistsAsync(roleName))
                        {
                            await roleManager.CreateAsync(new IdentityRole(roleName));
                        }
                    }

                    // Seed Admin User
                    var adminEmail = "admin@resumeanalyzer.ai";
                    var adminUser = await userManager.FindByEmailAsync(adminEmail);
                    if (adminUser == null)
                    {
                        var newAdmin = new ApplicationUser
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            FullName = "Lead Administrator",
                            EmailConfirmed = true,
                            SubscriptionPlanId = 3, // Enterprise plan
                            DateCreated = DateTime.UtcNow
                        };

                        var createAdmin = await userManager.CreateAsync(newAdmin, "AdminPass123!");
                        if (createAdmin.Succeeded)
                        {
                            await userManager.AddToRoleAsync(newAdmin, "Admin");
                        }
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
                    logger.LogError(ex, "An error occurred seeding the database.");
                }
            }

            await app.RunAsync();
        }

        // Polly Retry policy: retry 3 times with exponential backoff
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}
