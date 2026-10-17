using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TourManagement_MVC.Models;
using TourManagement_MVC.Models.ViewModels;
using TourManagement_MVC.Services;

namespace TourManagement_MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ITourService _tourService;

        public AdminController(ITourService tourService)
        {
            _tourService = tourService;
        }

        // GET: /Admin/Login
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User?.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            {
                return RedirectToAction("Dashboard");
            }
            return View(new AdminLoginViewModel());
        }

        // POST: /Admin/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public IActionResult Login(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var admin = _tourService.AuthenticateUser(model.Email.Trim(), model.Password);
            if (admin != null && string.Equals(admin.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, admin.FullName ?? admin.Email),
                    new Claim(ClaimTypes.Email, admin.Email),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                TempData["SuccessMessage"] = "Admin login successful! Welcome to the Admin Control Panel.";
                return RedirectToAction("Dashboard");
            }

            ModelState.AddModelError(string.Empty, "Invalid Admin credentials.");
            return View(model);
        }

        // GET: /Admin/Dashboard
        public IActionResult Dashboard()
        {
            ViewBag.TotalTours = _tourService.CountTours();
            ViewBag.TotalBookings = _tourService.CountBookings();
            ViewBag.TotalUsers = _tourService.CountUsers();
            ViewBag.RecentBookings = _tourService.GetRecentBookings(5);

            return View();
        }

        // GET: /Admin/AddTour
        public IActionResult AddTour()
        {
            return View(new Tour());
        }

        // POST: /Admin/AddTour
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddTour(Tour tour, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(tour);
            }

            _tourService.AddTour(tour, imageFile);
            TempData["SuccessMessage"] = $"Tour '{tour.TourName}' added successfully!";
            return RedirectToAction("ManageTours");
        }

        // GET: /Admin/EditTour/5
        public IActionResult EditTour(int id)
        {
            var tour = _tourService.GetTourById(id);
            if (tour == null) return NotFound();
            return View(tour);
        }

        // POST: /Admin/EditTour/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditTour(Tour tour, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(tour);
            }

            _tourService.UpdateTour(tour, imageFile);
            TempData["SuccessMessage"] = $"Tour '{tour.TourName}' updated successfully!";
            return RedirectToAction("ManageTours");
        }

        // POST: /Admin/DeleteTour/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTour(int id)
        {
            _tourService.DeleteTour(id);
            TempData["SuccessMessage"] = "Tour deleted successfully.";
            return RedirectToAction("ManageTours");
        }

        // GET: /Admin/ManageTours
        public IActionResult ManageTours()
        {
            var tours = _tourService.GetAllTours();
            return View(tours);
        }

        // GET: /Admin/AllBookings
        public IActionResult AllBookings()
        {
            var bookings = _tourService.GetAllBookings();
            return View(bookings);
        }

        // GET: /Admin/ManageUsers
        public IActionResult ManageUsers()
        {
            var users = _tourService.GetAllUsers();
            return View(users);
        }

        // POST: /Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(string email)
        {
            _tourService.DeleteUser(email);
            TempData["SuccessMessage"] = $"User '{email}' deleted successfully.";
            return RedirectToAction("ManageUsers");
        }

        // GET: /Admin/Logout
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Admin logged out successfully.";
            return RedirectToAction("Login");
        }

        // GET: /Admin/MigratePasswords
        [HttpGet]
        public IActionResult MigratePasswords()
        {
            var candidates = _tourService.MigratePlaintextPasswordsDetailed(false);
            ViewBag.Candidates = candidates;
            ViewBag.PlaintextCount = candidates?.Count ?? 0;
            return View();
        }

        // POST: /Admin/MigratePasswords
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MigratePasswordsPost()
        {
            var migrated = _tourService.MigratePlaintextPasswordsDetailed(true);
            ViewBag.Migrated = migrated;
            ViewBag.PlaintextCount = migrated?.Count ?? 0;
            TempData["SuccessMessage"] = $"Password migration completed. { (migrated?.Count ?? 0) } account(s) migrated to hashed passwords.";
            return View("MigratePasswords");
        }

        // GET: /Admin/ManageCounters
        [HttpGet]
        public IActionResult ManageCounters()
        {
            var counters = _tourService.GetAllCounters();
            ViewBag.Counters = counters;
            return View();
        }

        // POST: /Admin/ResetCounter
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetCounter(string name, int value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Counter name is required.";
                return RedirectToAction("ManageCounters");
            }

            _tourService.ResetCounter(name, value);
            TempData["SuccessMessage"] = $"Counter '{name}' reset to {value}.";
            return RedirectToAction("ManageCounters");
        }

        // GET: /Admin/RepairIds
        [HttpGet]
        public IActionResult RepairIds()
        {
            var messages = _tourService.RepairIdCollisions(false);
            ViewBag.Messages = messages;
            ViewBag.CanRepair = messages != null && messages.Count > 0;
            return View();
        }

        // POST: /Admin/RepairIds
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RepairIdsPost()
        {
            var results = _tourService.RepairIdCollisions(true);
            ViewBag.Messages = results;
            TempData["SuccessMessage"] = $"Repair completed. {results?.Count ?? 0} changes applied.";
            return View("RepairIds");
        }
    }
}
