using HotelManagement.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin,Manager")]
    public class HomeController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;

        public HomeController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            // Minimal dashboard numbers to validate DB connectivity
            var totalRooms = _dbContext.Rooms.Count();
            var totalBookings = _dbContext.Bookings.Count();
            var totalClients = _dbContext.Users.Count(u => u.Role == "Client");

            var displayName = User?.Identity?.Name ?? "Admin";

            ViewData["TotalRooms"] = totalRooms;
            ViewData["TotalBookings"] = totalBookings;
            ViewData["TotalClients"] = totalClients;
            ViewData["DisplayName"] = displayName;
            return View();
        }
    }
}
