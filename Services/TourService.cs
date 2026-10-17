using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TourManagement_MVC.Models;

namespace TourManagement_MVC.Services
{
    public class TourService : ITourService
    {
        private readonly IMongoCollection<Tour> _tours;
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Booking> _bookings;
        private readonly IMongoDatabase _database;
        private readonly IWebHostEnvironment _env;
        private readonly IPasswordHasher<User> _passwordHasher;

        public TourService(IConfiguration config, IWebHostEnvironment env, IPasswordHasher<User> passwordHasher)
        {
            _env = env;
            _passwordHasher = passwordHasher;

            // Connect to MongoDB
            // Prefer configuration from environment variables or user-secrets.
            // Supported keys (in order):
            // 1) configuration key "MongoDB:ConnectionString"
            // 2) connection string name "MongoDB" (ConnectionStrings:MongoDB)
            // 3) environment variable "MongoDB__ConnectionString"
            // 4) environment variable "MONGODB_CONNECTIONSTRING" or "MONGODB_URI"
            var connectionString = config["MongoDB:ConnectionString"]
                ?? config.GetConnectionString("MongoDB")
                ?? Environment.GetEnvironmentVariable("MongoDB__ConnectionString")
                ?? Environment.GetEnvironmentVariable("MONGODB_CONNECTIONSTRING")
                ?? Environment.GetEnvironmentVariable("MONGODB_URI");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("MongoDB connection string not configured. Set 'MongoDB:ConnectionString' in user secrets or environment variable 'MongoDB__ConnectionString'.");
            }

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(config["MongoDB:DatabaseName"]);

            _tours    = _database.GetCollection<Tour>("tours");
            _users    = _database.GetCollection<User>("users");
            _bookings = _database.GetCollection<Booking>("bookings");

            SeedData();
        }

        // Migrate plaintext passwords to hashed passwords.
        // If performMigration is false, returns number of accounts that would be migrated (dry-run).
        public int MigratePlaintextPasswords(bool performMigration)
        {
            var allUsers = _users.Find(_ => true).ToList();
            // ASP.NET Core Identity PasswordHasher output typically starts with "AQAAAAEA" prefix
            var candidates = allUsers.Where(u => string.IsNullOrEmpty(u.Password) || !u.Password.StartsWith("AQAAAAEA")).ToList();
            if (!performMigration) return candidates.Count;

            var migrationLog = _users.Database.GetCollection<MongoDB.Bson.BsonDocument>("migration_log");
            foreach (var user in candidates)
            {
                var plain = user.Password ?? string.Empty;
                // Hash the existing plaintext (if empty, leave as hash of empty)
                user.Password = _passwordHasher.HashPassword(user, plain);
                _users.ReplaceOne(u => u.Email.ToLower() == user.Email.ToLower(), user);

                var doc = new MongoDB.Bson.BsonDocument
                {
                    { "email", user.Email },
                    { "migratedAt", DateTime.UtcNow }
                };
                migrationLog.InsertOne(doc);
            }

            return candidates.Count;
        }

        public List<string> MigratePlaintextPasswordsDetailed(bool performMigration)
        {
            var allUsers = _users.Find(_ => true).ToList();
            var candidates = allUsers.Where(u => string.IsNullOrEmpty(u.Password) || !u.Password.StartsWith("AQAAAAEA")).ToList();
            var emails = candidates.Select(c => c.Email).ToList();
            if (!performMigration) return emails;

            var migrationLog = _users.Database.GetCollection<MongoDB.Bson.BsonDocument>("migration_log");
            foreach (var user in candidates)
            {
                var plain = user.Password ?? string.Empty;
                user.Password = _passwordHasher.HashPassword(user, plain);
                _users.ReplaceOne(u => u.Email.ToLower() == user.Email.ToLower(), user);

                var doc = new MongoDB.Bson.BsonDocument
                {
                    { "email", user.Email },
                    { "migratedAt", DateTime.UtcNow }
                };
                migrationLog.InsertOne(doc);
            }

            return emails;
        }

        // ── Seed default data only if collections are empty ──
        private void SeedData()
        {
            if (!_tours.Find(_ => true).Any())
            {
                _tours.InsertMany(new List<Tour>
                {
                    new Tour { TourId = 1, TourName = "Goa Beach & Sun Paradise",       Place = "Goa",                Days = 4, Price = 12500, Locations = "Baga Beach, Calangute, Aguada Fort, Panaji Cruise",          TourInfo = "Experience pristine sandy beaches, thrilling water sports, and sunset cruises across picturesque Goa.",                                Pic = "goa.jpg" },
                    new Tour { TourId = 2, TourName = "Kashmir Valley Enchantment",     Place = "Srinagar & Gulmarg", Days = 6, Price = 28900, Locations = "Dal Lake, Shikara Ride, Gulmarg Gondola, Pahalgam Valley",    TourInfo = "Immerse yourself in snow-capped Himalayan peaks, emerald valleys, and serene floating houseboats.",                                    Pic = "kashmir.jpg" },
                    new Tour { TourId = 3, TourName = "Kerala Backwaters & Hills",      Place = "Kerala",             Days = 5, Price = 21000, Locations = "Munnar Tea Gardens, Alleppey Houseboat, Thekkady Sanctuary",  TourInfo = "Relax amidst lush green tea plantations, tranquil lagoons, and world-renowned Ayurvedic retreats.",                                  Pic = "kerala.jpeg" },
                    new Tour { TourId = 4, TourName = "Himalayan Mountain Odyssey",     Place = "Manali & Solang",    Days = 5, Price = 18500, Locations = "Hadimba Temple, Solang Valley, Atal Tunnel, Rohtang Pass",   TourInfo = "Adventure packed with paragliding, snow skiing, and pine forests in the lap of Himachal Pradesh.",                                   Pic = "Landscape.jpg" },
                    new Tour { TourId = 5, TourName = "Golden Triangle Explorer",       Place = "Delhi, Agra, Jaipur",Days = 6, Price = 24500, Locations = "Taj Mahal, Red Fort, Amber Fort, Hawa Mahal, City Palace",   TourInfo = "Discover India's most celebrated heritage corridor with breathtaking Mughal and Rajput architecture.",                                 Pic = "TravelImage.jpg" }
                });

                // Ensure counters collection has current max tour id so GetNextSequence is atomic
                var counters = _database.GetCollection<BsonDocument>("counters");
                var filter = Builders<BsonDocument>.Filter.Eq("_id", "tourid");
                var doc = new BsonDocument {{ "_id", "tourid" }, { "seq", 5 }};
                counters.ReplaceOne(filter, doc, new ReplaceOptions { IsUpsert = true });
            }

            if (!_users.Find(_ => true).Any())
            {
                // Seed a default admin and a default regular user. Passwords are stored hashed.
                var admin = new User
                {
                    Email = "admin@gmail.com",
                    FirstName = "Admin",
                    LastName = "User",
                    Gender = "Other",
                    Phone = "0000000000",
                    DateOfBirth = DateTime.Today.AddYears(-30),
                    Street = "Admin Street",
                    City = "AdminCity",
                    State = "AdminState",
                    Role = "Admin"
                };
                admin.Password = _passwordHasher.HashPassword(admin, "admin");

                var user = new User
                {
                    Email = "user@gmail.com",
                    FirstName = "Rahul",
                    LastName = "Sharma",
                    Gender = "Male",
                    Phone = "9876543210",
                    DateOfBirth = new DateTime(1998, 5, 14),
                    Street = "12 Main Market Road",
                    City = "Ahmedabad",
                    State = "Gujarat",
                    Role = "User"
                };
                user.Password = _passwordHasher.HashPassword(user, "user123");

                _users.InsertMany(new[] { admin, user });

                // initialize booking counter to 0
                var counters = _database.GetCollection<BsonDocument>("counters");
                var bkFilter = Builders<BsonDocument>.Filter.Eq("_id", "bookingid");
                var bkDoc = new BsonDocument {{ "_id", "bookingid" }, { "seq", 0 }};
                counters.ReplaceOne(bkFilter, bkDoc, new ReplaceOptions { IsUpsert = true });
            }
        }

        // Atomically get next sequence number for a named counter using a counters collection
        private int GetNextSequence(string name)
        {
            var counters = _database.GetCollection<BsonDocument>("counters");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", name);
            var update = Builders<BsonDocument>.Update.Inc("seq", 1);
            var options = new FindOneAndUpdateOptions<BsonDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };
            var result = counters.FindOneAndUpdate(filter, update, options);
            if (result == null) return 1;
            return result.GetValue("seq", 0).ToInt32();
        }

        // ──────────────── TOUR METHODS ────────────────

        public List<Tour> GetAllTours()
            => _tours.Find(_ => true).SortBy(t => t.TourId).ToList();

        // Search with paging and sorting
        public System.Collections.Generic.List<Tour> SearchTours(TourManagement_MVC.Models.SearchQuery query, out long totalCount)
        {
            var builder = Builders<Tour>.Filter;
            var filters = new System.Collections.Generic.List<FilterDefinition<Tour>>();

            // Destination: match place, tourName or locations
            if (!string.IsNullOrWhiteSpace(query?.Destination))
            {
                var regex = new BsonRegularExpression(query.Destination, "i");
                filters.Add(builder.Or(
                    builder.Regex("place", regex),
                    builder.Regex("tourName", regex),
                    builder.Regex("locations", regex)));
            }

            // Price filter handled externally (controller passes maxPrice if needed); keep placeholder

            var filter = filters.Count == 0 ? builder.Empty : builder.And(filters);

            // Count total
            totalCount = _tours.CountDocuments(filter);

            // Sorting
            SortDefinition<Tour> sortDef = Builders<Tour>.Sort.Ascending(t => t.TourId);
            if (!string.IsNullOrWhiteSpace(query?.Sort))
            {
                if (query.Sort.Equals("price_asc", StringComparison.OrdinalIgnoreCase))
                    sortDef = Builders<Tour>.Sort.Ascending(t => t.Price);
                else if (query.Sort.Equals("price_desc", StringComparison.OrdinalIgnoreCase))
                    sortDef = Builders<Tour>.Sort.Descending(t => t.Price);
            }

            int page = Math.Max(1, query?.Page ?? 1);
            int pageSize = Math.Max(1, query?.PageSize ?? 12);
            int skip = (page - 1) * pageSize;

            var list = _tours.Find(filter).Sort(sortDef).Skip(skip).Limit(pageSize).ToList();
            return list;
        }

        // Return demo deals for non-stay search types. Reuses Tour model for demo purposes.
        public System.Collections.Generic.List<Tour> GetDealsForType(TourManagement_MVC.Models.SearchType type)
        {
            // For demo, pick some tours with small modifications based on type
            var all = GetAllTours();
            var rnd = new Random();
            var sample = all.OrderBy(t => rnd.Next()).Take(4).ToList();
            foreach (var t in sample)
            {
                switch (type)
                {
                    case TourManagement_MVC.Models.SearchType.Flight:
                        t.TourInfo = "Flight deal: discounted airfare plus hotel upgrades.";
                        t.Price = Math.Round(t.Price * 0.7m);
                        break;
                    case TourManagement_MVC.Models.SearchType.FlightHotel:
                        t.TourInfo = "Package deal: flight + hotel combo with savings.";
                        t.Price = Math.Round(t.Price * 0.75m);
                        break;
                    case TourManagement_MVC.Models.SearchType.CarRental:
                        t.TourInfo = "Car rental offers: free additional driver and insurance included.";
                        t.Price = Math.Round(t.Price * 0.5m);
                        break;
                    case TourManagement_MVC.Models.SearchType.Activities:
                        t.TourInfo = "Activity highlights and local experiences at special price.";
                        t.Price = Math.Round(t.Price * 0.6m);
                        break;
                    default:
                        break;
                }
            }
            return sample;
        }

        public Tour? GetTourById(int id)
            => _tours.Find(t => t.TourId == id).FirstOrDefault();

        public void AddTour(Tour tour, IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString("N")[..8] + Path.GetExtension(imageFile.FileName);
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "tours");
                Directory.CreateDirectory(uploadsFolder);
                using var stream = new FileStream(Path.Combine(uploadsFolder, fileName), FileMode.Create);
                imageFile.CopyTo(stream);
                tour.Pic = fileName;
            }
            else if (string.IsNullOrWhiteSpace(tour.Pic))
                tour.Pic = "TravelImage.jpg";

            // Assign atomic TourId using counters collection to avoid race conditions
            tour.TourId = GetNextSequence("tourid");
            _tours.InsertOne(tour);
        }

        public void UpdateTour(Tour tour, IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString("N")[..8] + Path.GetExtension(imageFile.FileName);
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "tours");
                Directory.CreateDirectory(uploadsFolder);
                using var stream = new FileStream(Path.Combine(uploadsFolder, fileName), FileMode.Create);
                imageFile.CopyTo(stream);
                tour.Pic = fileName;
            }

            var update = Builders<Tour>.Update
                .Set(t => t.TourName,  tour.TourName)
                .Set(t => t.Place,     tour.Place)
                .Set(t => t.Days,      tour.Days)
                .Set(t => t.Price,     tour.Price)
                .Set(t => t.Locations, tour.Locations)
                .Set(t => t.TourInfo,  tour.TourInfo)
                .Set(t => t.Pic,       tour.Pic);

            _tours.UpdateOne(t => t.TourId == tour.TourId, update);
        }

        public void DeleteTour(int id)
            => _tours.DeleteOne(t => t.TourId == id);

        // ──────────────── USER METHODS ────────────────

        public User? AuthenticateUser(string email, string password)
        {
            var user = _users.Find(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();
            if (user == null) return null;

            // Verify hashed password
            var verify = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
            if (verify == PasswordVerificationResult.Success || verify == PasswordVerificationResult.SuccessRehashNeeded)
            {
                if (verify == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    // Re-hash with current settings
                    user.Password = _passwordHasher.HashPassword(user, password);
                    _users.ReplaceOne(u => u.Email.ToLower() == user.Email.ToLower(), user);
                }
                return user;
            }

            // Do NOT compare or accept plain-text passwords. If verification fails, deny access.
            return null;
        }

        public bool RegisterUser(User user)
        {
            bool exists = _users.Find(u => u.Email.ToLower() == user.Email.ToLower()).Any();
            if (exists) return false;

            // Hash password before storing
            var plain = user.Password ?? string.Empty;
            user.Password = _passwordHasher.HashPassword(user, plain);
            if (string.IsNullOrWhiteSpace(user.Role)) user.Role = "User";
            _users.InsertOne(user);
            return true;
        }

        public User? GetUserByEmail(string email)
            => _users.Find(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();

        public List<User> GetAllUsers()
            => _users.Find(_ => true).ToList();

        public void DeleteUser(string email)
            => _users.DeleteOne(u => u.Email.ToLower() == email.ToLower());

        // ──────────────── BOOKING METHODS ────────────────

        public Booking CreateBooking(Booking booking)
        {
            // Use atomic counter for booking id to avoid collisions under concurrency
            booking.BookingId   = GetNextSequence("bookingid");
            booking.BookingDate = DateTime.Now;
            booking.Status      = "Confirmed";
            _bookings.InsertOne(booking);
            return booking;
        }

        // Async implementations
        public async Task<Booking> CreateBookingAsync(Booking booking)
        {
            booking.BookingId = GetNextSequence("bookingid");
            booking.BookingDate = DateTime.Now;
            booking.Status = "Confirmed";
            await _bookings.InsertOneAsync(booking);
            return booking;
        }

        public async Task<List<Booking>> GetBookingsByUserAsync(string email)
        {
            return await _bookings.Find(b => b.Email.ToLower() == email.ToLower())
                                  .SortByDescending(b => b.BookingDate)
                                  .ToListAsync();
        }

        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            return await _bookings.Find(_ => true).SortByDescending(b => b.BookingDate).ToListAsync();
        }

        public async Task<Booking?> GetBookingByIdAsync(int id)
        {
            return await _bookings.Find(b => b.BookingId == id).FirstOrDefaultAsync();
        }

        public System.Collections.Generic.Dictionary<string, int> GetAllCounters()
        {
            var counters = _database.GetCollection<BsonDocument>("counters");
            var list = counters.Find(_ => true).ToList();
            var dict = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var doc in list)
            {
                var id = doc.GetValue("_id").AsString;
                var seq = doc.GetValue("seq", 0).ToInt32();
                dict[id] = seq;
            }
            return dict;
        }

        public void ResetCounter(string name, int value)
        {
            var counters = _database.GetCollection<BsonDocument>("counters");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", name);
            var doc = new BsonDocument {{ "_id", name }, { "seq", value }};
            counters.ReplaceOne(filter, doc, new ReplaceOptions { IsUpsert = true });
        }

        public System.Collections.Generic.List<string> RepairIdCollisions(bool performRepair)
        {
            var messages = new System.Collections.Generic.List<string>();

            // Repair tours duplicates
            var tours = _tours.Find(_ => true).ToList();
            var tourGroups = tours.GroupBy(t => t.TourId).Where(g => g.Count() > 1);

            // read current tour counter without incrementing
            var counters = _database.GetCollection<BsonDocument>("counters");
            var tourCounterDoc = counters.Find(Builders<BsonDocument>.Filter.Eq("_id", "tourid")).FirstOrDefault();
            int nextTourSeq = tourCounterDoc?.GetValue("seq", 0).ToInt32() ?? 0;

            foreach (var g in tourGroups)
            {
                var id = g.Key;
                var items = g.ToList();
                // keep first, reassign others
                for (int i = 1; i < items.Count; i++)
                {
                    var doc = items[i];
                    int proposed;
                    if (performRepair)
                    {
                        proposed = GetNextSequence("tourid");
                        var update = Builders<Tour>.Update.Set(t => t.TourId, proposed);
                        _tours.UpdateOne(t => t.Id == doc.Id, update);
                        messages.Add($"Reassigned Tour _id={doc.Id} from {id} to {proposed}");
                    }
                    else
                    {
                        nextTourSeq++;
                        proposed = nextTourSeq;
                        messages.Add($"Would reassign Tour _id={doc.Id} from {id} to {proposed}");
                    }
                }
            }

            // Repair bookings duplicates
            var bookings = _bookings.Find(_ => true).ToList();
            var bookGroups = bookings.GroupBy(b => b.BookingId).Where(g => g.Count() > 1);
            var bookCounterDoc = counters.Find(Builders<BsonDocument>.Filter.Eq("_id", "bookingid")).FirstOrDefault();
            int nextBookSeq = bookCounterDoc?.GetValue("seq", 0).ToInt32() ?? 0;

            foreach (var g in bookGroups)
            {
                var id = g.Key;
                var items = g.ToList();
                for (int i = 1; i < items.Count; i++)
                {
                    var doc = items[i];
                    int proposed;
                    if (performRepair)
                    {
                        proposed = GetNextSequence("bookingid");
                        var update = Builders<Booking>.Update.Set(b => b.BookingId, proposed);
                        _bookings.UpdateOne(b => b.Id == doc.Id, update);
                        messages.Add($"Reassigned Booking _id={doc.Id} from {id} to {proposed}");
                    }
                    else
                    {
                        nextBookSeq++;
                        proposed = nextBookSeq;
                        messages.Add($"Would reassign Booking _id={doc.Id} from {id} to {proposed}");
                    }
                }
            }

            if (!performRepair)
            {
                if (messages.Count == 0) messages.Add("No collisions detected.");
            }

            return messages;
        }

        public List<Booking> GetBookingsByUser(string email)
            => _bookings.Find(b => b.Email.ToLower() == email.ToLower())
                        .SortByDescending(b => b.BookingDate)
                        .ToList();

        public List<Booking> GetAllBookings()
            => _bookings.Find(_ => true).SortByDescending(b => b.BookingDate).ToList();

        // Count methods
        public int CountTours()
            => (int)_tours.CountDocuments(FilterDefinition<Tour>.Empty);

        public int CountBookings()
            => (int)_bookings.CountDocuments(FilterDefinition<Booking>.Empty);

        public int CountUsers()
            => (int)_users.CountDocuments(FilterDefinition<User>.Empty);

        // Return the most recent N bookings without loading the full collection
        public List<Booking> GetRecentBookings(int count)
            => _bookings.Find(_ => true).SortByDescending(b => b.BookingDate).Limit(count).ToList();

        public void CancelBooking(int bookingId)
        {
            var update = Builders<Booking>.Update.Set(b => b.Status, "Cancelled");
            _bookings.UpdateOne(b => b.BookingId == bookingId, update);
        }
    }
}
