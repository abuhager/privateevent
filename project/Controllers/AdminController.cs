using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Models;

namespace project.Controllers
{
    // هذا السطر يقوم بحماية الكلاس بالكامل
    // أي شخص ليس "Admin" سيتم طرده قبل الوصول لأي دالة هنا
    [SessionCheck("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public AdminController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // --- لوحة التحكم (Dashboard) ---
        public IActionResult Dashboard()
        {
            // 1. عدد الطلاب
            ViewBag.StudentCount = _dbContext.Users.Count(u => u.Role == "Student");

            // 2. عدد الفعاليات القادمة فقط
            ViewBag.EventCount = _dbContext.Events.Count(e => e.Date >= DateTime.Now);

            // 3. عدد الحجوزات النشطة للفعاليات القادمة (مؤشر للإقبال الحالي)
            ViewBag.ActiveBookings = _dbContext.Rolls
                .Include(r => r.Event)
                .Count(r => r.States == "active" && r.Event.Date >= DateTime.Now);

            // 4. السعة الكلية المتاحة مستقبلاً
            ViewBag.TotalCapacity = _dbContext.Events
                .Where(e => e.Date >= DateTime.Now)
                .Sum(e => e.Limit);

            // جلب القائمة مرتبة للأحدث
            var events = _dbContext.Events.OrderByDescending(e => e.Date).ToList();
            return View(events);
        }

        // --- إضافة فعالية جديدة ---
        public IActionResult CreateEvent()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateEvent(Event model)
        {
            // إزالة الـ Rolls من التحقق لأننا لا نرسلها عند الإنشاء
            ModelState.Remove("Rolls");

            if (ModelState.IsValid)
            {
                // منع إضافة تاريخ في الماضي
                if (model.Date < DateTime.Now)
                {
                    ModelState.AddModelError("Date", "لا يمكن إضافة فعالية بتاريخ قديم");
                    return View(model);
                }

                _dbContext.Events.Add(model);
                _dbContext.SaveChanges();
                TempData["msg"] = "تم إضافة الفعالية بنجاح";
                return RedirectToAction("Dashboard");
            }
            return View(model);
        }

        // --- تعديل فعالية ---
        public IActionResult EditEvent(int id)
        {
            var ev = _dbContext.Events.Find(id);
            if (ev == null) return NotFound();
            return View(ev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditEvent(Event model)
        {
            ModelState.Remove("Rolls");

            if (ModelState.IsValid)
            {
                // يمكن إضافة شرط التاريخ هنا أيضاً إذا أردت منع تعديل التاريخ للماضي
                _dbContext.Events.Update(model);
                _dbContext.SaveChanges();
                TempData["msg"] = "تم تعديل الفعالية بنجاح";
                return RedirectToAction("Dashboard");
            }
            return View(model);
        }

        // --- حذف فعالية ---
        public IActionResult DeleteEvent(int id)
        {
            var ev = _dbContext.Events.Find(id);
            if (ev == null) return NotFound();
            return View(ev);
        }

        [HttpPost, ActionName("DeleteEvent")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var ev = _dbContext.Events.Find(id);
            if (ev != null)
            {
                _dbContext.Events.Remove(ev);
                _dbContext.SaveChanges();
                TempData["msg"] = "تم حذف الفعالية وحجوزاتها بنجاح";
            }
            return RedirectToAction("Dashboard");
        }

        // --- عرض الطلاب المسجلين في فعالية محددة ---
        public IActionResult ViewStudents(int id)
        {
            var ev = _dbContext.Events
                .Include(e => e.Rolls)
                .ThenInclude(r => r.User)
                .FirstOrDefault(e => e.Id == id);

            if (ev == null) return NotFound();

            // إرسال عدد الحضور الفعلي (Active) فقط للفيو ليظهر بجانب العنوان مثلاً
            ViewBag.ActiveCount = ev.Rolls.Count(r => r.States == "active");
            ViewBag.WaitingCount = ev.Rolls.Count(r => r.States == "waiting");
            ViewBag.CancelCount = ev.Rolls.Count(r => r.States == "cancelled");

            

            return View(ev);
        }
    }
}