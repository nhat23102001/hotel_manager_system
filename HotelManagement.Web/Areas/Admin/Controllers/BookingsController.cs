using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelManagement.Web.Data;
using HotelManagement.Web.DTOs;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace HotelManagement.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin,Manager")]
    public class BookingsController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;
        private readonly IEmailSender _emailSender;

        public BookingsController(HotelManagementDbContext dbContext, IEmailSender emailSender)
        {
            _dbContext = dbContext;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index(string? searchCode, string? status)
        {
            var query = _dbContext.Bookings.Include(b => b.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchCode))
            {
                query = query.Where(b => b.BookingCode.Contains(searchCode) || b.GuestName.Contains(searchCode));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(b => b.Status == status);
            }

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .Select(b => new BookingDto
                {
                    Id = b.Id,
                    BookingCode = b.BookingCode,
                    GuestName = b.GuestName,
                    PhoneNumber = b.PhoneNumber,
                    Email = b.Email,
                    BookingDate = b.BookingDate,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    TotalNights = b.TotalNights,
                    TotalAmount = b.TotalAmount,
                    Status = b.Status,
                    Username = b.User != null ? b.User.Username : null
                })
                .ToListAsync();

            ViewBag.SearchCode = searchCode;
            ViewBag.Status = status;

            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _dbContext.Bookings
                .Include(b => b.User)
                .Include(b => b.Details)
                    .ThenInclude(d => d.Room)
                        .ThenInclude(r => r.RoomType)
                .Include(b => b.Services)
                    .ThenInclude(s => s.Service)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            var model = new BookingDto
            {
                Id = booking.Id,
                BookingCode = booking.BookingCode,
                GuestName = booking.GuestName,
                PhoneNumber = booking.PhoneNumber,
                Email = booking.Email,
                Address = booking.Address,
                BookingDate = booking.BookingDate,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                TotalNights = booking.TotalNights,
                SubTotal = booking.SubTotal,
                VAT = booking.VAT,
                TotalAmount = booking.TotalAmount,
                Status = booking.Status,
                Username = booking.User?.Username
            };

            ViewBag.BookingDetails = booking.Details;
            ViewBag.BookingServices = booking.Services;

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var booking = await _dbContext.Bookings
                .Include(b => b.Details)
                    .ThenInclude(d => d.Room)
                        .ThenInclude(r => r.RoomType)
                .Include(b => b.Services)
                    .ThenInclude(s => s.Service)
                .Include(b => b.User)
                    .ThenInclude(u => u.Profile)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = status;
            await _dbContext.SaveChangesAsync();

            // Send confirmation email when status is Confirmed
            var recipientEmail = booking.Email ?? booking.User?.Profile?.Email ?? booking.User?.Username;
            if (status == "Confirmed" && !string.IsNullOrWhiteSpace(recipientEmail))
            {
                var subject = $"Rolax Hotel thông báo - {booking.GuestName}";
                var body = BuildBookingEmailBody(booking);

                try
                {
                    await _emailSender.SendEmailAsync(recipientEmail, subject, body);
                }
                catch (Exception ex)
                {
                    // Không chặn luồng cập nhật trạng thái; chỉ ghi nhận cảnh báo
                    TempData["Warning"] = "Cập nhật trạng thái thành công nhưng gửi email thất bại. Vui lòng kiểm tra cấu hình SMTP.";
                    // TODO: log ex if logging is configured
                }
            }

            TempData["Success"] = "Cập nhật trạng thái thành công.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var booking = await _dbContext.Bookings
                .Include(b => b.Details)
                .Include(b => b.Services)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            _dbContext.Bookings.Remove(booking);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Xóa đặt phòng thành công.";
            return RedirectToAction(nameof(Index));
        }

        private string BuildBookingEmailBody(HotelManagement.Web.Entities.Booking booking)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Rolax Hotel thông báo");
            sb.AppendLine($"Gửi anh/chị: {booking.GuestName}");
            sb.AppendLine();

            sb.AppendLine("Chi tiết đặt phòng:");
            sb.AppendLine($"- Mã đặt phòng: {booking.BookingCode}");
            sb.AppendLine($"- Ngày đặt: {booking.BookingDate:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"- Nhận phòng: {booking.CheckInDate:dd/MM/yyyy}");
            sb.AppendLine($"- Trả phòng: {booking.CheckOutDate:dd/MM/yyyy}");
            sb.AppendLine($"- Trạng thái: {booking.Status}");
            sb.AppendLine();

            sb.AppendLine("Danh sách phòng:");
            foreach (var d in booking.Details)
            {
                sb.AppendLine($"  • Phòng: {d.Room?.RoomName} ({d.Room?.RoomCode}) - Loại: {d.Room?.RoomType?.Name} - Giá/đêm: {d.PricePerNight:N0} VNĐ");
            }
            sb.AppendLine();

            if (booking.Services != null && booking.Services.Any())
            {
                sb.AppendLine("Dịch vụ đi kèm:");
                foreach (var s in booking.Services)
                {
                    sb.AppendLine($"  • {s.Service?.Name}: {s.Quantity} {s.Service?.Unit} - {s.TotalPrice:N0} VNĐ");
                }
                sb.AppendLine();
            }

            sb.AppendLine("Tổng kết:");
            sb.AppendLine($"- Tạm tính: {booking.SubTotal:N0} VNĐ");
            sb.AppendLine($"- VAT (10%): {booking.VAT:N0} VNĐ");
            sb.AppendLine($"- Tổng cộng: {booking.TotalAmount:N0} VNĐ");
            sb.AppendLine();

            sb.AppendLine("Thanh toán khi trả phòng.");
            sb.AppendLine();
            sb.AppendLine("Cảm ơn quý khách!");
            sb.AppendLine("Liên hệ:");
            sb.AppendLine("email: rolaxhotel@gmail.com");
            sb.AppendLine("phone: +84 (28) 3914 5456");

            return sb.ToString();
        }
    }
}
