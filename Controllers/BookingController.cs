using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using TourManagement_MVC.Models;
using TourManagement_MVC.Services;

namespace TourManagement_MVC.Controllers
{
    public class BookingController : Controller
    {
        private readonly ITourService _tourService;
        private readonly IEmailService _emailService;

        public BookingController(ITourService tourService, IEmailService emailService)
        {
            _tourService = tourService;
            _emailService = emailService;
        }

        // GET: /Booking/Create?tourId=1
        public IActionResult Create(int? tourId)
        {
            var booking = new Booking();

            if (tourId.HasValue)
            {
                var tour = _tourService.GetTourById(tourId.Value);
                if (tour != null)
                {
                    booking.TourName = tour.TourName;
                    booking.Place = tour.Place;
                    ViewBag.Tour = tour;
                }
            }

            // Pre-fill from authenticated user (claims preferred, fallback to session)
            string? userEmail = User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value
                                ?? HttpContext.Session.GetString("UserEmail");
            string? userName = User?.Identity?.Name ?? HttpContext.Session.GetString("UserName");
            if (!string.IsNullOrEmpty(userEmail))
            {
                booking.Email = userEmail;
                booking.FirstName = userName ?? "";
            }

            return View(booking);
        }

        // POST: /Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Booking booking)
        {
            if (!ModelState.IsValid)
            {
                return View(booking);
            }

            // Basic validation: ensure travel dates make sense
            if (booking.TravelEndDate <= booking.TravelStartDate)
            {
                ModelState.AddModelError(string.Empty, "Check-out date must be after check-in date.");
                return View(booking);
            }

            var created = _tourService.CreateBooking(booking);
            // Send confirmation email (fire-and-forget)
            _ = _emailService.SendBookingConfirmationAsync(created.Email, $"Booking Confirmed - {created.TourName}",
                $"<p>Dear {created.FirstName},</p><p>Your booking for <strong>{created.TourName}</strong> from {created.TravelStartDate:d} to {created.TravelEndDate:d} for {created.Adults} adult(s) and {created.Children} children in {created.Rooms} room(s) has been confirmed. Booking ID: {created.BookingId}.</p>");
            TempData["SuccessMessage"] = $"Congratulations! Your tour '{booking.TourName}' has been successfully booked.";
            return RedirectToAction("Confirmation", new { id = created.BookingId });
        }

        // GET: /Booking/Confirmation/5
        public async System.Threading.Tasks.Task<IActionResult> Confirmation(int id)
        {
            var booking = await _tourService.GetBookingByIdAsync(id);
            if (booking == null)
            {
                return RedirectToAction("MyBookings");
            }
            return View(booking);
        }

        // GET: /Booking/MyBookings
        public IActionResult MyBookings()
        {
            // Determine current user email (claims preferred, fallback to session)
            string? userEmail = User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value
                                ?? HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Please log in to view your bookings.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("MyBookings", "Booking") });
            }

            var bookings = _tourService.GetBookingsByUser(userEmail);
            return View(bookings);
        }

        // POST: /Booking/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            // Determine current user email (claims preferred, fallback to session)
            string? userEmail = User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value
                                ?? HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Please log in to manage your bookings.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("MyBookings", "Booking") });
            }

            var booking = _tourService.GetAllBookings().FirstOrDefault(b => b.BookingId == id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            if (!string.Equals(booking.Email, userEmail, StringComparison.OrdinalIgnoreCase))
            {
                // Prevent users from cancelling others' bookings
                TempData["ErrorMessage"] = "You are not authorized to cancel this booking.";
                return RedirectToAction("MyBookings");
            }

            _tourService.CancelBooking(id);
            // Send cancellation email
            _ = _emailService.SendBookingCancellationAsync(booking.Email, $"Booking Cancelled - {booking.TourName}",
                $"<p>Dear {booking.FirstName},</p><p>Your booking for <strong>{booking.TourName}</strong> from {booking.TravelStartDate:d} to {booking.TravelEndDate:d} (Booking ID: {booking.BookingId}) has been cancelled.</p>");
            TempData["SuccessMessage"] = "Your booking was successfully cancelled.";
            return RedirectToAction("MyBookings");
        }
    }
}
