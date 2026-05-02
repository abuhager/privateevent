using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Models;
using ClosedXML.Excel;

namespace project.Controllers
{
    [SessionCheck("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public AdminController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IActionResult Dashboard()
        {
            var events = _dbContext.Events.ToList();

            var eventTitles = events.Select(e => e.Title).ToList();
            var bookingCounts = events.Select(e =>
                _dbContext.Rolls.Count(r => r.EventId == e.Id && r.States == "active")
            ).ToList();
            var capacities = events.Select(e => e.Limit).ToList();

            ViewBag.EventTitles = System.Text.Json.JsonSerializer.Serialize(eventTitles);
            ViewBag.BookingCounts = System.Text.Json.JsonSerializer.Serialize(bookingCounts);
            ViewBag.Capacities = System.Text.Json.JsonSerializer.Serialize(capacities);

            // ← هاد اللي كان ناقص
            ViewBag.StudentCount = _dbContext.Users.Count(u => u.Role == "Student");
            ViewBag.EventCount = events.Count;

            ViewBag.ActiveBookings = _dbContext.Rolls.Count(r => r.States == "active");
            ViewBag.CheckedIn = _dbContext.Rolls.Count(r => r.States == "checkedin");
            ViewBag.TotalCapacity = events.Sum(e => e.Limit);
            return View(events);
        }

        public IActionResult CreateEvent()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateEvent(Event model)
        {
            ModelState.Remove("Rolls");

            if (ModelState.IsValid)
            {
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

            if (!ModelState.IsValid)
                return View(model);

            var existingEvent = _dbContext.Events.FirstOrDefault(e => e.Id == model.Id);
            if (existingEvent == null)
                return NotFound();

            int oldLimit = existingEvent.Limit;

            int confirmedCount = _dbContext.Rolls.Count(r =>
                r.EventId == existingEvent.Id &&
                (r.States == "active" || r.States == "checkedin"));

            if (model.Limit < confirmedCount)
            {
                ModelState.AddModelError("Limit",
                    $"لا يمكن تقليل السعة إلى {model.Limit} لأن عدد الحجوزات المؤكدة/الحضور الحالي هو {confirmedCount}.");

                return View(model);
            }

            existingEvent.Title = model.Title;
            existingEvent.Description = model.Description;
            existingEvent.Date = model.Date;
            existingEvent.StartTime = model.StartTime;
            existingEvent.Location = model.Location;
            existingEvent.major = model.major;
            existingEvent.Limit = model.Limit;

            if (existingEvent.Limit > oldLimit)
            {
                int availableSpots = existingEvent.Limit - confirmedCount;

                if (availableSpots > 0)
                {
                    var waitingList = _dbContext.Rolls
                        .Where(r => r.EventId == existingEvent.Id && r.States == "waiting")
                        .OrderBy(r => r.BookingTime)
                        .Take(availableSpots)
                        .ToList();

                    foreach (var roll in waitingList)
                    {
                        roll.States = "active";
                    }
                }
            }

            _dbContext.SaveChanges();
            TempData["msg"] = "تم تعديل الفعالية بنجاح";
            return RedirectToAction("Dashboard");
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

        public IActionResult ViewStudents(int id)
        {
            var ev = _dbContext.Events
                .Include(e => e.Rolls)
                .ThenInclude(r => r.User)
                .FirstOrDefault(e => e.Id == id);

            if (ev == null) return NotFound();

            ViewBag.ActiveCount = ev.Rolls.Count(r => r.States == "active");
            ViewBag.CheckedInCount = ev.Rolls.Count(r => r.States == "checkedin");
            ViewBag.WaitingCount = ev.Rolls.Count(r => r.States == "waiting");
            ViewBag.CancelCount = ev.Rolls.Count(r => r.States == "cancelled");

            return View(ev);
        }
        public IActionResult ExportBookings(int eventId)
        {
            var ev = _dbContext.Events.Find(eventId);
            if (ev == null) return NotFound();

            var rolls = _dbContext.Rolls
                .Where(r => r.EventId == eventId)
                .Include(r => r.User)
                .ToList();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("الحجوزات");

            sheet.Cell(1, 1).Value = "رقم الحجز";
            sheet.Cell(1, 2).Value = "اسم الطالب";
            sheet.Cell(1, 3).Value = "البريد الإلكتروني";
            sheet.Cell(1, 4).Value = "الحالة";
            sheet.Cell(1, 5).Value = "اسم الفعالية";

            var headerRow = sheet.Range("A1:E1");
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#198754");
            headerRow.Style.Font.FontColor = XLColor.White;

            for (int i = 0; i < rolls.Count; i++)
            {
                int row = i + 2;
                sheet.Cell(row, 1).Value = rolls[i].Id;
                sheet.Cell(row, 2).Value = rolls[i].User?.Name ?? "-";
                sheet.Cell(row, 3).Value = rolls[i].User?.Email ?? "-";
                sheet.Cell(row, 4).Value = rolls[i].States;
                sheet.Cell(row, 5).Value = ev.Title;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"bookings-{ev.Title}-{DateTime.Now:yyyyMMdd}.xlsx";
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        public IActionResult VerifyTicket(int? rollId)
        {
            if (rollId == null)
                return View();

            var roll = _dbContext.Rolls
                .Include(r => r.Event)
                .Include(r => r.User)
                .FirstOrDefault(r => r.Id == rollId);

            ViewBag.Searched = true;

            if (roll == null)
            {
                ViewBag.Status = "invalid";
                ViewBag.Message = "❌ هذه التذكرة غير موجودة في النظام";
                return View();
            }

            if (roll.States == "active")
            {
                ViewBag.Status = "valid";
                ViewBag.Message = "✅ حجز صحيح ومؤكد";
            }
            else if (roll.States == "checkedin")
            {
                ViewBag.Status = "checkedin";
                ViewBag.Message = "🔵 تم تسجيل حضور هذا الشخص مسبقاً";
            }
            else if (roll.States == "cancelled")
            {
                ViewBag.Status = "cancelled";
                ViewBag.Message = "⚠️ هذا الحجز تم إلغاؤه";
            }
            else if (roll.States == "waiting")
            {
                ViewBag.Status = "waiting";
                ViewBag.Message = "⏳ هذا الشخص في قائمة الانتظار";
            }
            else
            {
                ViewBag.Status = "invalid";
                ViewBag.Message = "❌ حالة التذكرة غير معروفة";
            }

            ViewBag.Roll = roll;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckInTicket(int rollId)
        {
            var roll = _dbContext.Rolls.FirstOrDefault(r => r.Id == rollId);

            if (roll == null)
            {
                TempData["VerifyMessage"] = "❌ هذه التذكرة غير موجودة";
                return RedirectToAction("VerifyTicket");
            }

            if (roll.States == "checkedin")
            {
                TempData["VerifyMessage"] = "✅ تم تسجيل الحضور مسبقاً";
                return RedirectToAction("VerifyTicket", new { rollId = rollId });
            }

            if (roll.States != "active")
            {
                TempData["VerifyMessage"] = "⚠️ لا يمكن تأكيد حضور هذه التذكرة";
                return RedirectToAction("VerifyTicket", new { rollId = rollId });
            }

            roll.States = "checkedin";
            _dbContext.SaveChanges();

            TempData["VerifyMessage"] = "✅ تم تأكيد الحضور بنجاح";
            return RedirectToAction("VerifyTicket", new { rollId = rollId });
        }
    }
}