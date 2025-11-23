using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Models;

namespace project.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        public StudentController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IActionResult EventsList()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Log", "Login");
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var events = _dbContext.Events.ToList();

            var statusDict = new Dictionary<int, string>();
            foreach (var ev in events)
            {
                var roll = _dbContext.Rolls.FirstOrDefault(r => r.EventId == ev.Id && r.UserId == userId);
                statusDict[ev.Id] = roll != null ? roll.States : "غير مسجل";
            }

            ViewBag.StatusDict = statusDict;
            return View(events);
        }
        [HttpGet]
        public IActionResult BookEvent(int eventId)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Log", "Login");

            var ev = _dbContext.Events.Find(eventId);
            if (ev == null) return NotFound();

            int userId = HttpContext.Session.GetInt32("UserId").Value;
            var alreadyBooked = _dbContext.Rolls.Any(r => r.EventId == eventId && r.UserId == userId && r.States == "active");
            ViewBag.IsBooked = alreadyBooked;

            return View(ev); 
        }
        [HttpPost]
        [ActionName("BookEvent")]
        public IActionResult BookEventPost(int eventId)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Log", "Login");

            int userId = HttpContext.Session.GetInt32("UserId").Value;
            var exists = _dbContext.Rolls.Any(r => r.EventId == eventId && r.UserId == userId && r.States == "active");
            if (exists)
            {
                TempData["msg"] = "أنت حاجز هذه الفعالية بالفعل!";
                return RedirectToAction("EventDetails", new { eventId = eventId });
            }

            var roll = new Roll
            {
                EventId = eventId,
                UserId = userId,
                States = "active"
            };
            _dbContext.Rolls.Add(roll);
            _dbContext.SaveChanges();

            TempData["msg"] = "تم الحجز بنجاح!";
            return RedirectToAction("BookEvent", new { eventId = eventId });
        }
        public IActionResult CancelBooking(int eventId)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Log", "Login");

            int userId = HttpContext.Session.GetInt32("UserId").Value;
            var roll = _dbContext.Rolls.FirstOrDefault(r => r.EventId == eventId && r.UserId == userId && r.States == "active");
            if (roll != null)
            {
                roll.States = "cancelled";
                _dbContext.SaveChanges();
                TempData["msg"] = "تم إلغاء الحجز.";
            }
            else
            {
                TempData["msg"] = "لا يوجد حجز نشط لإلغائه.";
            }
            return RedirectToAction("BookEvent", new { eventId = eventId });
        }


    }
}
