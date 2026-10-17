using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using TourManagement_MVC.Models;

namespace TourManagement_MVC.Services
{
    public interface ITourService
    {
        // Tour operations
        List<Tour> GetAllTours();
        Tour? GetTourById(int id);
        void AddTour(Tour tour, IFormFile? imageFile);
        void UpdateTour(Tour tour, IFormFile? imageFile);
        void DeleteTour(int id);

        // User operations
        User? AuthenticateUser(string email, string password);
        bool RegisterUser(User user);
        User? GetUserByEmail(string email);
        List<User> GetAllUsers();
        void DeleteUser(string email);

        // Booking operations
        Booking CreateBooking(Booking booking);
        List<Booking> GetBookingsByUser(string email);
        List<Booking> GetAllBookings();
        // Search with paging and sorting
        System.Collections.Generic.List<Tour> SearchTours(TourManagement_MVC.Models.SearchQuery query, out long totalCount);
        // Deals for non-stay search types (demo/stub)
        System.Collections.Generic.List<Tour> GetDealsForType(TourManagement_MVC.Models.SearchType type);
        // Async variants
        System.Threading.Tasks.Task<Booking> CreateBookingAsync(Booking booking);
        System.Threading.Tasks.Task<System.Collections.Generic.List<Booking>> GetBookingsByUserAsync(string email);
        System.Threading.Tasks.Task<System.Collections.Generic.List<Booking>> GetAllBookingsAsync();
        System.Threading.Tasks.Task<Booking?> GetBookingByIdAsync(int id);
        // Counts (more efficient than loading all documents)
        int CountTours();
        int CountBookings();
        int CountUsers();
        // Recent bookings retrieval
        System.Collections.Generic.List<Booking> GetRecentBookings(int count);
        void CancelBooking(int bookingId);

        // Migrate any users that have plaintext passwords to hashed passwords.
        // If performMigration is false, returns the number of accounts that would be migrated.
        int MigratePlaintextPasswords(bool performMigration);
        // Returns emails of users that would be (or were) migrated when performMigration=true
        System.Collections.Generic.List<string> MigratePlaintextPasswordsDetailed(bool performMigration);
        // Counters operations
        System.Collections.Generic.Dictionary<string, int> GetAllCounters();
        void ResetCounter(string name, int value);

        // Repair duplicate numeric IDs (tours/bookings). Returns messages describing actions (dry-run when performRepair=false).
        System.Collections.Generic.List<string> RepairIdCollisions(bool performRepair);
    }
}
