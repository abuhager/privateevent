using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Models;
using ClosedXML.Excel;

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
            var events = _dbContext.Events.ToList();

            // ✅ جمع بيانات الرسم البياني
            // أسماء الفعاليات (للمحور الأفقي)
            var eventTitles = events.Select(e => e.Title).ToList();

            // عدد الحجوزات النشطة لكل فعالية
            var bookingCounts = events.Select(e =>
                _dbContext.Rolls.Count(r => r.EventId == e.Id && r.States == "active")
            ).ToList();

            // الطاقة الاستيعابية لكل فعالية (Limit)
            var capacities = events.Select(e => e.Limit).ToList();

            // ✅ تمرير البيانات للـ View
            ViewBag.EventTitles = System.Text.Json.JsonSerializer.Serialize(eventTitles);
            ViewBag.BookingCounts = System.Text.Json.JsonSerializer.Serialize(bookingCounts);
            ViewBag.Capacities = System.Text.Json.JsonSerializer.Serialize(capacities);

            // إجماليات للـ KPI cards
            ViewBag.TotalEvents = events.Count;
            ViewBag.TotalBookings = _dbContext.Rolls.Count(r => r.States == "active");
            ViewBag.TotalUsers = _dbContext.Users.Count();

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

public IActionResult ExportBookings(int eventId)
    {
        var ev = _dbContext.Events.Find(eventId);
        if (ev == null) return NotFound();

        // ✅ جلب الحجوزات مع بيانات الطلاب
        var rolls = _dbContext.Rolls
            .Where(r => r.EventId == eventId)
            .Include(r => r.User)
            .ToList();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("الحجوزات");

        // ✅ العناوين
        sheet.Cell(1, 1).Value = "رقم الحجز";
        sheet.Cell(1, 2).Value = "اسم الطالب";
        sheet.Cell(1, 3).Value = "البريد الإلكتروني";
        sheet.Cell(1, 4).Value = "الحالة";
        sheet.Cell(1, 5).Value = "اسم الفعالية";

        // ✅ تنسيق العناوين
        var headerRow = sheet.Range("A1:E1");
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#198754");
        headerRow.Style.Font.FontColor = XLColor.White;

        // ✅ البيانات
        for (int i = 0; i < rolls.Count; i++)
        {
            int row = i + 2;
            sheet.Cell(row, 1).Value = rolls[i].Id;
            sheet.Cell(row, 2).Value = rolls[i].User?.Name ?? "-";
            sheet.Cell(row, 3).Value = rolls[i].User?.Email ?? "-";
            sheet.Cell(row, 4).Value = rolls[i].States;
            sheet.Cell(row, 5).Value = ev.Title;
        }

        // ✅ ضبط عرض الأعمدة تلقائياً
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"bookings-{ev.Title}-{DateTime.Now:yyyyMMdd}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
}