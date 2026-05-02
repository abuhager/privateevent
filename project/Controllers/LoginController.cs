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
        [HttpPost]
        public IActionResult Log(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            // 1. إذا المستخدم غير موجود أصلاً
            if (user == null)
            {
                ViewBag.Error = "بيانات الدخول غير صحيحة";
                return View();
            }

            bool isPasswordCorrect = false;

            // 2. الفحص الذكي والآمن (Smart & Safe Check)
            // الشرط الجديد: لازم يبدأ بـ $2 ويكون طوله 60 حرف عشان نعتبره مشفر
            if (user.Password.StartsWith("$2") && user.Password.Length == 60)
            {
                // الحالة أ: الباسورد مشفر حقيقي -> نستخدم Verify
                try
                {
                    isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, user.Password);
                }
                catch
                {
                    isPasswordCorrect = false;
                }
            }
            else
            {
                // الحالة ب: الباسورد عادي (أو صدفة مثل "$200Dollars") -> مقارنة مباشرة
                isPasswordCorrect = (user.Password == password);

                // الحالة ج: التصحيح التلقائي (Auto-Fix)
                // بما أن المستخدم دخل بنجاح، سنقوم بتشفير الباسورد الآن وحفظه
                if (isPasswordCorrect)
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(password);
                    _context.Users.Update(user);
                    _context.SaveChanges();
                }
            }

            // 3. النتيجة النهائية للفحص
            if (!isPasswordCorrect)
            {
                ViewBag.Error = "بيانات الدخول غير صحيحة";
                return View();
            }

            // 4. إعداد الجلسة (Session)
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserRole", user.Role);

            if (user.Role == "Admin")
            {
                return RedirectToAction("Index2", "User");
            }
            else
            {
                return RedirectToAction("Index1", "User");
            }
        }
    }
}