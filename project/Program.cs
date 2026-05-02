using Microsoft.EntityFrameworkCore;
using project.Models;


namespace project
{
    public class Program
    {
        public static void Main(string[] args)
 {
         var builder = WebApplication.CreateBuilder(args);


            // Add services to the container.
 builder.Services.AddRazorPages();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            }); builder.Services.AddHttpContextAccessor();


            // Add DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
   options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        var app = builder.Build();
            app.MapControllerRoute(
          name: "default",
          pattern: "{controller=Home}/{action=Index}/{id?}");

            // Apply pending migrations and seed data
            using (var scope = app.Services.CreateScope())
  {
   var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
     dbContext.Database.Migrate();

  // Seed initial data
 SeedDatabase(dbContext);
 }

        // Configure the HTTP request pipeline.
   if (!app.Environment.IsDevelopment())
 {
  app.UseExceptionHandler("/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

 app.UseHttpsRedirection();
  app.UseStaticFiles();
app.UseSession();
    
       app.UseRouting();

     app.UseAuthorization();

    app.MapRazorPages();

      app.Run();
      }

        private static void SeedDatabase(ApplicationDbContext dbContext)
        {
            // ????? ????? ?????????? ????? ???? ???????
            var users = new List<User>
    {
        new User
        {
            Name = "Admin User",
            Email = "admin@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = "Admin",
            UImage = null
        }, 
        new User
        {
            Name = "Admin User2",
            Email = "admin2@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Admin123!2"),
            Role = "Admin",
            UImage = null
        },
        new User
        {
            Name = "Student User",
            Email = "student@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Student123!"),
            Role = "Student",
            UImage = null
        },
        new User
        {
            Name = "Student User2",
            Email = "student2@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Student123!2"),
            Role = "Student",
            UImage = null
        },
        new User
        {
            Name = "Student User3",
            Email = "student3@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Student123!3"),
            Role = "Student",
            UImage = null
        }
    };

            foreach (var user in users)
            {
                if (!dbContext.Users.Any(u => u.Email == user.Email))
                {
                    dbContext.Users.Add(user);
                }
            }

            dbContext.SaveChanges();
        }
    }
}
