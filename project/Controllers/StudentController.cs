using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Models;

namespace project.Controllers
{
    [SessionCheck("Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public StudentController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IActionResult EventsList(string searchString)
        {
            int userId = HttpContext.Session.GetInt32("UserId").Value;

            var eventsQuery = _dbContext.Events
    .Include(e => e.Rolls.Where(r => r.UserId == userId))
    .OrderByDescending(e => e.Date)      // رتب تنازلياً حسب التاريخ
    .ThenByDescending(e => e.StartTime)  // ثم تنازلياً حسب الوقت
    .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                eventsQuery = eventsQuery.Where(e => e.Title.Contains(searchString) || e.Location.Contains(searchString));
            }

            return View(eventsQuery.ToList());
        }

        [HttpGet]
        public IActionResult BookEvent(int eventId)
        {
            var ev = _dbContext.Events.Find(eventId);
            if (ev == null) return NotFound();

            int userId = HttpContext.Session.GetInt32("UserId").Value;

            var existingRoll = _dbContext.Rolls
                .FirstOrDefault(r => r.EventId == eventId && r.UserId == userId && (r.States == "active" || r.States == "waiting"));

            ViewBag.UserStatus = existingRoll?.States;
            ViewBag.CurrentCount = _dbContext.Rolls.Count(r => r.EventId == eventId && r.States == "active");

            return View(ev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("BookEvent")]
        public IActionResult BookEventPost(int eventId)
        {
            int userId = HttpContext.Session.GetInt32("UserId").Value;

            var ev = _dbContext.Events.Find(eventId);
            if (ev == null) return NotFound();

            var existingRoll = _dbContext.Rolls.FirstOrDefault(r => r.EventId == eventId && r.UserId == userId);

            if (existingRoll != null && (existingRoll.States == "active" || existingRoll.States == "waiting"))
            {
                TempData["msg"] = "أنت مسجل في هذه الفعالية مسبقاً!";
                return RedirectToAction("BookEvent", new { eventId = eventId });
            }

            int activeBookings = _dbContext.Rolls.Count(r => r.EventId == eventId && r.States == "active");

            string newStatus = "active";
            string message = "تم تأكيد الحجز بنجاح!";

            if (activeBookings >= ev.Limit)
            {
                newStatus = "waiting";
                message = "اكتمل العدد، تم إضافتك إلى قائمة الانتظار.";
            }

            if (existingRoll != null)
            {
                existingRoll.States = newStatus;
                existingRoll.BookingTime = DateTime.Now;
            }
            else
            {
                var roll = new Roll
                {
                    EventId = eventId,
                    UserId = userId,
                    States = newStatus,
                    BookingTime = DateTime.Now
                };
                _dbContext.Rolls.Add(roll);
            }

            _dbContext.SaveChanges();
            TempData["msg"] = message;
            return RedirectToAction("BookEvent", new { eventId = eventId });
        }

        public IActionResult CancelBooking(int eventId)
        {
            int userId = HttpContext.Session.GetInt32("UserId").Value;

            var currentRoll = _dbContext.Rolls.FirstOrDefault(r => r.EventId == eventId && r.UserId == userId && r.States == "active");

            if (currentRoll != null)
            {
                currentRoll.States = "cancelled";

                var nextInQueue = _dbContext.Rolls
                    .Where(r => r.EventId == eventId && r.States == "waiting")
                    .OrderBy(r => r.BookingTime)
                    .FirstOrDefault();

                if (nextInQueue != null)
                {
                    nextInQueue.States = "active";
                }

                _dbContext.SaveChanges();
                TempData["msg"] = nextInQueue != null ? "تم إلغاء حجزك ودخول طالب من الانتظار مكانك." : "تم إلغاء الحجز.";
            }
            else
            {
                var waitingRoll = _dbContext.Rolls.FirstOrDefault(r => r.EventId == eventId && r.UserId == userId && r.States == "waiting");
                if (waitingRoll != null)
                {
                    waitingRoll.States = "cancelled";
                    _dbContext.SaveChanges();
                    TempData["msg"] = "تم الانسحاب من قائمة الانتظار.";
                }
            }

            return RedirectToAction("BookEvent", new { eventId = eventId });
        }

        public IActionResult MyBookings()
        {
            int userId = HttpContext.Session.GetInt32("UserId").Value;

            var myBookings = _dbContext.Rolls
                .Include(r => r.Event)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Event.Date)
                .ToList();

            return View(myBookings);
        }
    }
}