using Microsoft.AspNetCore.Mvc;
using project.Models;

namespace project.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoginController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Log()

        {
             if (HttpContext.Session.GetString("UserRole") != null)
                return RedirectToAction("index", "Home");
            

                return View(); 
        }

        [HttpPost]
        public IActionResult Log(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
            if (user == null)
            {
                ViewBag.Error = "unvalid data";
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserRole", user.Role); 
            if (user.Role == "Admin")
            {
                return RedirectToAction("Index2", "User");
            }
            else 
            {
                return RedirectToAction("Index", "User");
            }

        }
    }
}