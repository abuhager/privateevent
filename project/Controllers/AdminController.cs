using Microsoft.AspNetCore.Mvc;
using project.Models;

namespace project.Controllers
{
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
            return View(events);
        }
        public IActionResult CreateEvent()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateEvent(Event model)
        {
            if (ModelState.IsValid)
            {
                _dbContext.Events.Add(model);
                _dbContext.SaveChanges();
                return RedirectToAction("Dashboard");
            }
            return View(model);
        }

    }
}
