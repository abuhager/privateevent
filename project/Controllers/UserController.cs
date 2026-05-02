using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using project.Models;



namespace project.Controllers

{




    public class UserController : Controller

    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)

        {
            _context = context;
        }
        public IActionResult Index1()

        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Log", "Login");
            string role = HttpContext.Session.GetString("UserRole");
            if (role == "Student")
                return View();
            else
                return RedirectToAction("Index2", "User");


        }
        public IActionResult Index2()

        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Log", "Login");
            string role=HttpContext.Session.GetString("UserRole");
            if (role== "Admin")
            return View();
            else
                return RedirectToAction("Index1", "User");

        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();


            return RedirectToAction("Index", "Home");


        }

        public IActionResult Profile()

        {

            var userId = HttpContext.Session.GetInt32("UserId");
            var user = _context.Users.Find(userId);

            if (user == null) return NotFound();

            return View(user);

        }
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Log", "Login");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            // 1. التحقق من السشن
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Log", "Login");

            // 2. التحقق من المدخلات
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["error"] = "جميع الحقول مطلوبة!";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["error"] = "كلمة المرور الجديدة وتأكيدها غير متطابقين!";
                return View();
            }

            // 3. جلب المستخدم
            var user = _context.Users.Find(userId);
            if (user == null) return NotFound();

            // 4. التحقق من كلمة المرور الحالية
            // بما أننا متأكدين أنها تشفرت عند الدخول، نستخدم Verify مباشرة
            bool isPasswordCorrect = false;
            
           isPasswordCorrect = BCrypt.Net.BCrypt.Verify(currentPassword, user.Password);
            

            if (!isPasswordCorrect)
            {
                TempData["error"] = "كلمة المرور الحالية غير صحيحة!";
                return View();
            }

            // 5. حفظ الجديدة
            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.Users.Update(user);
            _context.SaveChanges();

            TempData["msg"] = "تم تغيير كلمة المرور بنجاح!";
            return RedirectToAction("Profile");
        }

    }

}