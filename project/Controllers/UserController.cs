using Microsoft.AspNetCore.Mvc;

namespace project.Controllers
{
    public class UserController : Controller
    {
       
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Log", "Login");
            }
            return View();
        }
        
        public IActionResult Index2()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Log", "Login");
            }
            return View();
        }
        public IActionResult Logout()
        {

            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");


        }
    }
}
