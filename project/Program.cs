using Microsoft.EntityFrameworkCore;
using project.Models;
using QuestPDF.Infrastructure;

namespace project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false; // ?

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorPages();
            builder.Services.AddControllersWithViews();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
                SeedDatabase(dbContext);
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        private static void SeedDatabase(ApplicationDbContext dbContext)
        {
            var users = new List<User>
            {
                new User { Name = "Admin User",    Email = "admin@example.com",    Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),    Role = "Admin",   UImage = null },
                new User { Name = "Admin User2",   Email = "admin2@example.com",   Password = BCrypt.Net.BCrypt.HashPassword("Admin123!2"),   Role = "Admin",   UImage = null },
                new User { Name = "Student User",  Email = "student@example.com",  Password = BCrypt.Net.BCrypt.HashPassword("Student123!"),  Role = "Student", UImage = null },
                new User { Name = "Student User2", Email = "student2@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Student123!2"), Role = "Student", UImage = null },
                new User { Name = "Student User3", Email = "student3@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Student123!3"), Role = "Student", UImage = null }
            };

            foreach (var user in users)
            {
                if (!dbContext.Users.Any(u => u.Email == user.Email))
                    dbContext.Users.Add(user);
            }

            dbContext.SaveChanges();
        }
    }
}