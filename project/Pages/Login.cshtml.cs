using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using project.Models;

namespace project.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            ErrorMessage = TempData["Error"] as string;
        }

        public IActionResult OnPost(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ErrorMessage = "البريد الإلكتروني وكلمة المرور مطلوبة";
                return Page();
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user == null)
            {
                ErrorMessage = "بيانات دخول غير صحيحة";
                return Page();
            }

            // Set session or authentication here
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.Name);

            return RedirectToPage("User/Index");
        }
    }
}
