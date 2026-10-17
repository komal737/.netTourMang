using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using TourManagement_MVC.Services;

namespace TourManagement_MVC.Controllers
{
    public class TourController : Controller
    {
        private readonly ITourService _tourService;

        public TourController(ITourService tourService)
        {
            _tourService = tourService;
        }

        // GET: /Tour/Suggestions?q=...
        [HttpGet]
        public IActionResult Suggestions(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return Json(new string[0]);

            var tours = _tourService.GetAllTours();
            var matches = tours
                .SelectMany(t => new[] { t.Place, t.TourName }
                    .Concat((t.Locations ?? string.Empty).Split(',').Select(s => s.Trim())))
                .Where(s => !string.IsNullOrWhiteSpace(s) && s.Contains(q, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();

            return Json(matches);
        }

        // GET: /Tour/Index
    public IActionResult Index([FromQuery] TourManagement_MVC.Models.SearchQuery query, decimal? maxPrice)
        {
            // Ensure defaults
            if (query == null) query = new TourManagement_MVC.Models.SearchQuery();

            ViewBag.SearchTerm = query.Destination;
            ViewBag.SelectedPlace = query.Destination;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Page = query.Page;
            ViewBag.PageSize = query.PageSize;
            ViewBag.Sort = query.Sort;

            // Route based on search type
            if (query.Type == TourManagement_MVC.Models.SearchType.Stay)
            {
                long totalCount;
                var tours = _tourService.SearchTours(query, out totalCount);
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
                return View(tours);
            }
            else
            {
                // For other types return demo deals (uses Tour model for demo)
                var deals = _tourService.GetDealsForType(query.Type);
                ViewBag.Deals = deals;
                // show empty tours list but view will render deals if present
                return View(new System.Collections.Generic.List<TourManagement_MVC.Models.Tour>());
            }
        }

        // GET: /Tour/Details/5
        public IActionResult Details(int id)
        {
            var tour = _tourService.GetTourById(id);
            if (tour == null)
            {
                return NotFound();
            }
            return View(tour);
        }
    }
}
