using HotelManagement.Web.Data;
using HotelManagement.Web.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Areas.Client.Controllers
{
    [Area("Client")]
    public class HomeController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;

        public HomeController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var rooms = await _dbContext.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.IsActive && r.Status == "Available")
                .OrderBy(r => r.PricePerNight)
                .Take(6)
                .Select(r => new RoomDto
                {
                    Id = r.Id,
                    RoomCode = r.RoomCode,
                    RoomName = r.RoomName,
                    RoomType = r.RoomType != null ? r.RoomType.Name : string.Empty,
                    PricePerNight = r.PricePerNight,
                    MaxPeople = r.MaxPeople,
                    Status = r.Status,
                    ImageUrl = r.ImageUrl
                })
                .ToListAsync();

            return View(rooms);
        }
    }
}
