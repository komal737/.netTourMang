using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TourManagement_MVC.Models;
using TourManagement_MVC.Services;

using TourManagement_MVC.Models.ViewModels;

namespace TourManagement_MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ITourService _tourService;

        public HomeController(ITourService tourService)
        {
            _tourService = tourService;
        }

        public IActionResult Index()
        {
            var featuredTours = _tourService.GetAllTours().Take(3).ToList();
            ViewBag.TotalTours = _tourService.GetAllTours().Count;
            ViewBag.TotalBookings = _tourService.GetAllBookings().Count;
            ViewBag.TotalUsers = _tourService.GetAllUsers().Count;
            return View(featuredTours);
        }

        public IActionResult About()
        {
            ViewBag.TotalTours = _tourService.GetAllTours().Count;
            ViewBag.TotalBookings = _tourService.GetAllBookings().Count;
            ViewBag.TotalUsers = _tourService.GetAllUsers().Count;
            return View();
        }

        // GET: /Home/Contact
        public IActionResult Contact()
        {
            return View(new ContactViewModel());
        }

        private readonly IEmailService _emailService;

        // POST: /Home/Contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<IActionResult> Contact(ContactViewModel model, [FromServices] IEmailService emailService)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Send contact message to site admin
            var adminEmail = HttpContext.RequestServices.GetService<Microsoft.Extensions.Configuration.IConfiguration>()?["Email:Admin"] ?? "admin@example.com";
            await emailService.SendContactMessageAsync(adminEmail, $"Website Contact: {model.Subject}",
                $"<p>From: {model.FullName} &lt;{model.Email}&gt;</p><p>Message:</p><div>{System.Net.WebUtility.HtmlEncode(model.Message)}</div>");

            TempData["SuccessMessage"] = $"Thank you, {model.FullName}! Your message regarding '{model.Subject}' has been received. Our travel specialist will contact you shortly at {model.Email}.";
            return RedirectToAction("Contact");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
