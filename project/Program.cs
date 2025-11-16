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
            builder.Services.AddSession();
            builder.Services.AddHttpContextAccessor();


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
       if (!dbContext.Users.Any())
         {
                var users = new List<User>
{
    new User
    {
        Name = "Admin User",
        Email = "admin@example.com",
        Password = "Admin123!",
        Role = "Admin",
                UImage=null

    },
    new User
    {
        Name = "Student User",
        Email = "student@example.com",
        Password = "Student123!",
        Role = "Student",
        UImage=null

    }
};

                dbContext.Users.AddRange(users);


                dbContext.SaveChanges();
         }
     }
    }
}
