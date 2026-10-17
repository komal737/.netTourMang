using System;

namespace TourManagement_MVC.Models
{
    public enum SearchType
    {
        Stay = 0,
        Flight = 1,
        FlightHotel = 2,
        CarRental = 3,
        Activities = 4
    }

    public class SearchQuery
    {
        public SearchType Type { get; set; } = SearchType.Stay;
        public string? Destination { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public int Adults { get; set; } = 2;
        public int Children { get; set; } = 0;
        public int Rooms { get; set; } = 1;
        // optional fields for other types
        public string? FlightFrom { get; set; }
        public string? CarPickup { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        // sort: price_asc, price_desc, default = relevance
        public string? Sort { get; set; }
    }
}
