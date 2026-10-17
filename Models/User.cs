using System;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TourManagement_MVC.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Key]
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        [BsonElement("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        [BsonElement("lastName")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Gender is required")]
        [BsonElement("gender")]
        public string Gender { get; set; } = "Male";

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [BsonElement("password")]
        public string Password { get; set; } = string.Empty;

        [BsonElement("role")]
        public string Role { get; set; } = "User";

        [Required(ErrorMessage = "Mobile number is required")]
        [Display(Name = "Mobile Number")]
        [Phone(ErrorMessage = "Invalid mobile number")]
        [BsonElement("phone")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        [BsonElement("dateOfBirth")]
        public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-20);

        [Required]
        [BsonElement("street")]
        public string Street { get; set; } = string.Empty;

        [Required]
        [BsonElement("city")]
        public string City { get; set; } = string.Empty;

        [Required]
        [BsonElement("state")]
        public string State { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
