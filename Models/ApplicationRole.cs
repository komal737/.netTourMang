using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;

namespace TourManagement_MVC.Models
{
    [CollectionName("roles")]
    public class ApplicationRole : MongoIdentityRole
    {
    }
}
