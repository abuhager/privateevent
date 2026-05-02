using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Models;
using QuestPDF.Helpers;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

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
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.StartTime)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                eventsQuery = eventsQuery.Where(e =>
                    e.Title.Contains(searchString) || e.Location.Contains(searchString));
            }

            return View(eventsQuery.ToList());
        }

        [HttpGet]
        public IActionResult BookEvent(int eventId)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Log", "Login");

            var ev = _dbContext.Events.Find(eventId);
            if (ev == null) return NotFound();

            int userId = HttpContext.Session.GetInt32("UserId").Value;

            var roll = _dbContext.Rolls
                .FirstOrDefault(r => r.EventId == eventId && r.UserId == userId);

            ViewBag.UserStatus = roll?.States;
            ViewBag.CurrentCount = _dbContext.Rolls.Count(r =>
                r.EventId == eventId &&
                (r.States == "active" || r.States == "checkedin"));
            ViewBag.RollId = roll?.Id;

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

            var existingRoll = _dbContext.Rolls
                .FirstOrDefault(r => r.EventId == eventId && r.UserId == userId);

            if (existingRoll != null &&
                (existingRoll.States == "active" ||
                 existingRoll.States == "waiting" ||
                 existingRoll.States == "checkedin"))
            {
                TempData["msg"] = "أنت مسجل في هذه الفعالية مسبقاً!";
                return RedirectToAction("BookEvent", new { eventId = eventId });
            }

            int activeBookings = _dbContext.Rolls.Count(r =>
                r.EventId == eventId &&
                (r.States == "active" || r.States == "checkedin"));

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

            var currentRoll = _dbContext.Rolls.FirstOrDefault(r =>
                r.EventId == eventId &&
                r.UserId == userId &&
                (r.States == "active" || r.States == "checkedin"));

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
                TempData["msg"] = nextInQueue != null
                    ? "تم إلغاء حجزك ودخول طالب من الانتظار مكانك."
                    : "تم إلغاء الحجز.";
            }
            else
            {
                var waitingRoll = _dbContext.Rolls.FirstOrDefault(r =>
                    r.EventId == eventId &&
                    r.UserId == userId &&
                    r.States == "waiting");

                if (waitingRoll != null)
                {
                    waitingRoll.States = "cancelled";
                    _dbContext.SaveChanges();
                    TempData["msg"] = "تم الانسحاب من قائمة الانتظار.";
                }
            }

            return RedirectToAction("BookEvent", new { eventId = eventId });
        }

        public IActionResult MyTicket(int rollId)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Log", "Login");

            int userId = HttpContext.Session.GetInt32("UserId").Value;

            var roll = _dbContext.Rolls
                .Include(r => r.Event)
                .Include(r => r.User)
                .FirstOrDefault(r => r.Id == rollId && r.UserId == userId);

            if (roll == null || (roll.States != "active" && roll.States != "checkedin"))
            {
                TempData["msg"] = "لا توجد تذكرة صالحة بهذا الرقم.";
                return RedirectToAction("EventsList");
            }

        
            ViewBag.Roll = roll;

            return View(roll.Event);
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

        public IActionResult DownloadTicket(int eventId)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Log", "Login");

            int userId = HttpContext.Session.GetInt32("UserId").Value;

            var user = _dbContext.Users.Find(userId);
            var ev = _dbContext.Events.Find(eventId);
            var roll = _dbContext.Rolls.FirstOrDefault(r =>
                r.EventId == eventId &&
                r.UserId == userId &&
                (r.States == "active" || r.States == "checkedin"));

            if (user == null || ev == null || roll == null)
                return NotFound();

            var arabicStyle = TextStyle.Default
                .FontFamily("Arial")
                .FontSize(12);

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(arabicStyle);

                    page.Content().Column(col =>
                    {
                        col.Item().AlignCenter()
                            .Text("تذكرة فعالية")
                            .FontSize(28).Bold().FontColor(Colors.Green.Medium);

                        col.Item().PaddingVertical(10)
                            .LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().AlignCenter().PaddingBottom(20)
                            .Text(ev.Title).FontSize(22).Bold();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Padding(8).Text("اسم الطالب").Bold();
                            table.Cell().Padding(8).Text(user.Name);

                            table.Cell().Padding(8).Text("التاريخ").Bold();
                            table.Cell().Padding(8).Text(ev.Date.ToString("yyyy-MM-dd"));

                            table.Cell().Padding(8).Text("وقت البداية").Bold();
                            table.Cell().Padding(8).Text(ev.StartTime.ToString(@"hh\:mm"));

                            table.Cell().Padding(8).Text("الموقع").Bold();
                            table.Cell().Padding(8).Text(ev.Location);

                            table.Cell().Padding(8).Text("التخصص").Bold();
                            table.Cell().Padding(8).Text(ev.major);

                            table.Cell().Padding(8).Text("رقم الحجز").Bold();
                            table.Cell().Padding(8).Text($"#{roll.Id:D5}");

                            table.Cell().Padding(8).Text("الحالة").Bold();
                            table.Cell().Padding(8).Text(roll.States == "checkedin" ? "تم الحضور" : "حجز مؤكد");
                        });

                        col.Item().PaddingVertical(10)
                            .LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().AlignCenter().PaddingTop(10)
                            .Text("يرجى إبراز هذه التذكرة عند الحضور")
                            .FontSize(10).Italic().FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf", $"ticket-{roll.Id}.pdf");
        }
    }
}