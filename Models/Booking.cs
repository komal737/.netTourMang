using System;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TourManagement_MVC.Models
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class Booking
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("bookingId")]
        public int BookingId { get; set; }

        [Required]
        [Display(Name = "Tour Package")]
        [BsonElement("tourName")]
        public string TourName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Destination")]
        [BsonElement("place")]
        public string Place { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Traveler Email")]
        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Traveler Name")]
        [BsonElement("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Mobile Number")]
        [BsonElement("phone")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Booking Date")]
        [BsonElement("bookingDate")]
        public DateTime BookingDate { get; set; } = DateTime.Now;

        // Travel date range
        [Required]
        [Display(Name = "Check-in")]
        [DataType(DataType.Date)]
        [BsonElement("travelStart")]
        public DateTime TravelStartDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Check-out")]
        [DataType(DataType.Date)]
        [BsonElement("travelEnd")]
        public DateTime TravelEndDate { get; set; } = DateTime.Today.AddDays(1);

        // Occupancy
        [Required]
        [Display(Name = "Adults")]
        [Range(1, 10)]
        [BsonElement("adults")]
        public int Adults { get; set; } = 2;

        [Display(Name = "Children")]
        [Range(0, 10)]
        [BsonElement("children")]
        public int Children { get; set; } = 0;

        [Required]
        [Display(Name = "Rooms")]
        [Range(1, 10)]
        [BsonElement("rooms")]
        public int Rooms { get; set; } = 1;

        [Display(Name = "Status")]
        [BsonElement("status")]
        public string Status { get; set; } = "Confirmed";
    }
}
