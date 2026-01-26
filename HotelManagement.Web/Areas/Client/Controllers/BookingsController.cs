using HotelManagement.Web.Data;
using HotelManagement.Web.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HotelManagement.Web.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize(AuthenticationSchemes = "ClientAuth", Roles = "Client")]
    public class BookingsController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;

        public BookingsController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> MyBookings(string? search, string? status)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Forbid();
            }

            IQueryable<HotelManagement.Web.Entities.Booking> query = _dbContext.Bookings
                .AsNoTracking()
                .Include(b => b.Details)
                .ThenInclude(bd => bd.Room)
                .Where(b => b.UserId == userId);

            // Filter by search (booking code)
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b => b.BookingCode.Contains(search));
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(b => b.Status == status);
            }

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Statuses = new[] 
            { 
                new { Value = "Pending", Text = "Chờ xác nhận" },
                new { Value = "Confirmed", Text = "Đã xác nhận" },
                new { Value = "Cancelled", Text = "Đã hủy" },
                new { Value = "Completed", Text = "Hoàn thành" }
            };

            return View(bookings);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Forbid();
            }

            var booking = await _dbContext.Bookings
                .AsNoTracking()
                .Include(b => b.Details)
                .ThenInclude(bd => bd.Room)
                .Include(b => b.Services)
                .ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Forbid();
            }

            var booking = await _dbContext.Bookings
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            // Only allow cancellation if status is Pending or Confirmed
            if (booking.Status != "Pending" && booking.Status != "Confirmed")
            {
                TempData["Error"] = "Không thể hủy đặt phòng với trạng thái này";
                return RedirectToAction("Details", new { id = id });
            }

            booking.Status = "Cancelled";
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Hủy đặt phòng thành công";
            return RedirectToAction("Details", new { id = id });
        }
    }
}
