using Microsoft.EntityFrameworkCore;
using project.Models;
using QuestPDF.Infrastructure;

namespace project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var webRootPath = builder.Environment.WebRootPath
                ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
            var arabicFontPath = Path.Combine(
                webRootPath,
                "fonts",
                "NotoSansArabic.ttf");

            using (var arabicFont = File.OpenRead(arabicFontPath))
                QuestPDF.Drawing.FontManager.RegisterFont(arabicFont);

            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;

            builder.Services.AddRazorPages();
            builder.Services.AddControllersWithViews();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
            });

            var databaseProvider = builder.Configuration["Database:Provider"] ?? "SqlServer";
            var useInMemoryDatabase = databaseProvider.Equals(
                "InMemory",
                StringComparison.OrdinalIgnoreCase);
            var seedDemoData = useInMemoryDatabase ||
                builder.Configuration.GetValue<bool>("Database:SeedDemoData");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                if (useInMemoryDatabase)
                {
                    options.UseInMemoryDatabase("UniEventsDemo");
                    return;
                }

                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (useInMemoryDatabase)
                    dbContext.Database.EnsureCreated();
                else
                    dbContext.Database.Migrate();

                SeedDatabase(dbContext, includeDemoData: seedDemoData);
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            if (!app.Environment.IsDevelopment())
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

        private static void SeedDatabase(
            ApplicationDbContext dbContext,
            bool includeDemoData = false)
        {
            if (!includeDemoData)
                return;

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

            for (var index = 4; index <= 12; index++)
            {
                var email = $"student{index}@example.com";

                if (dbContext.Users.Any(user => user.Email == email))
                    continue;

                dbContext.Users.Add(new User
                {
                    Name = $"Student User {index}",
                    Email = email,
                    Password = BCrypt.Net.BCrypt.HashPassword("Student123!"),
                    Role = "Student",
                    UImage = null
                });
            }

            dbContext.SaveChanges();

            if (dbContext.Events.Any())
                return;

            var events = new List<Event>
            {
                new Event
                {
                    Title = "ندوة الذكاء الاصطناعي",
                    Description = "جلسة تطبيقية حول استخدام الذكاء الاصطناعي في الدراسة والعمل.",
                    Date = DateTime.Today.AddDays(7),
                    StartTime = new TimeSpan(11, 0, 0),
                    Location = "قاعة المؤتمرات",
                    major = "جميع التخصصات",
                    Limit = 24
                },
                new Event
                {
                    Title = "ورشة الأمن السيبراني",
                    Description = "مقدمة عملية في حماية الحسابات والأنظمة من التهديدات الشائعة.",
                    Date = DateTime.Today.AddDays(12),
                    StartTime = new TimeSpan(10, 0, 0),
                    Location = "مختبر الشبكات",
                    major = "تكنولوجيا المعلومات",
                    Limit = 12
                },
                new Event
                {
                    Title = "مسابقة البرمجة الجامعية",
                    Description = "تحديات خوارزميات وحل مشكلات لفرق الطلبة.",
                    Date = DateTime.Today.AddDays(18),
                    StartTime = new TimeSpan(9, 30, 0),
                    Location = "كلية العلوم وتكنولوجيا المعلومات",
                    major = "علوم الحاسوب",
                    Limit = 3
                },
                new Event
                {
                    Title = "يوم التوظيف المفتوح",
                    Description = "لقاءات مباشرة مع شركات تقنية وفرص تدريب وتوظيف.",
                    Date = DateTime.Today.AddDays(25),
                    StartTime = new TimeSpan(10, 30, 0),
                    Location = "الصالة الرياضية",
                    major = "جميع التخصصات",
                    Limit = 40
                }
            };

            dbContext.Events.AddRange(events);
            dbContext.SaveChanges();

            var students = dbContext.Users
                .Where(user => user.Role == "Student")
                .OrderBy(user => user.Id)
                .ToList();

            var rolls = new List<Roll>();
            var bookingTime = DateTime.Now.AddDays(-4);

            void AddBookings(Event ev, int count, string state, int studentOffset = 0)
            {
                for (var index = 0; index < count; index++)
                {
                    rolls.Add(new Roll
                    {
                        UserId = students[(index + studentOffset) % students.Count].Id,
                        EventId = ev.Id,
                        States = state,
                        BookingTime = bookingTime.AddMinutes(rolls.Count * 18)
                    });
                }
            }

            AddBookings(events[0], 7, "active");
            AddBookings(events[0], 1, "checkedin", studentOffset: 7);
            AddBookings(events[1], 6, "active", studentOffset: 2);
            AddBookings(events[2], 3, "active");
            AddBookings(events[2], 2, "waiting", studentOffset: 3);
            AddBookings(events[3], 5, "active", studentOffset: 4);

            dbContext.Rolls.AddRange(rolls);

            dbContext.SaveChanges();
        }
    }
}
