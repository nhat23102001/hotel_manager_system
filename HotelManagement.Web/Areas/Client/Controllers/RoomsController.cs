using HotelManagement.Web.Data;
using HotelManagement.Web.DTOs;
using HotelManagement.Web.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HotelManagement.Web.Areas.Client.Controllers
{
    [Area("Client")]
    public class RoomsController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;

        public RoomsController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(int? roomTypeId, decimal? minPrice, decimal? maxPrice, string? search, DateTime? checkInDate, DateTime? checkOutDate, int page = 1)
        {
            int pageSize = 9;
            var query = _dbContext.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.IsActive && r.Status == "Available")
                .AsQueryable();

            // Filter by search
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => r.RoomCode.Contains(search) ||
                                       (r.RoomName != null && r.RoomName.Contains(search)) ||
                                       (r.Description != null && r.Description.Contains(search)));
            }

            // Filter by room type
            if (roomTypeId.HasValue && roomTypeId.Value > 0)
            {
                query = query.Where(r => r.RoomTypeId == roomTypeId.Value);
            }

            // Filter by price range
            if (minPrice.HasValue)
            {
                query = query.Where(r => r.PricePerNight >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(r => r.PricePerNight <= maxPrice.Value);
            }

            // Filter by availability (check booking conflicts)
            if (checkInDate.HasValue && checkOutDate.HasValue)
            {
                // Get rooms that have conflicting bookings
                var bookedRoomIds = await _dbContext.BookingDetails
                    .Include(bd => bd.Booking)
                    .Where(bd => bd.Booking.Status != "Cancelled" &&
                                 bd.Booking.CheckInDate < checkOutDate.Value &&
                                 bd.Booking.CheckOutDate > checkInDate.Value)
                    .Select(bd => bd.RoomId)
                    .Distinct()
                    .ToListAsync();

                // Filter out booked rooms
                query = query.Where(r => !bookedRoomIds.Contains(r.Id));
            }

            // Count total rooms
            var totalRooms = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalRooms / pageSize);

            // Validate page number
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var rooms = await query
                .OrderBy(r => r.PricePerNight)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RoomDto
                {
                    Id = r.Id,
                    RoomCode = r.RoomCode,
                    RoomName = r.RoomName,
                    RoomTypeId = r.RoomTypeId,
                    RoomType = r.RoomType != null ? r.RoomType.Name : "",
                    PricePerNight = r.PricePerNight,
                    MaxPeople = r.MaxPeople,
                    Status = r.Status,
                    Description = r.Description,
                    ImageUrl = r.ImageUrl,
                    IsActive = r.IsActive
                })
                .ToListAsync();

            // Load room types for filter dropdown
            ViewBag.RoomTypes = await _dbContext.RoomTypes
                .Where(rt => rt.IsActive)
                .Select(rt => new SelectListItem { Value = rt.Id.ToString(), Text = rt.Name })
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.RoomTypeId = roomTypeId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CheckInDate = checkInDate?.ToString("yyyy-MM-dd");
            ViewBag.CheckOutDate = checkOutDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRooms = totalRooms;

            return View(rooms);
        }

        public async Task<IActionResult> Details(int id)
        {
            var room = await _dbContext.Rooms
                .AsNoTracking()
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive && r.Status == "Available");

            if (room == null)
            {
                return NotFound();
            }

            // Get user info if authenticated
            string? userEmail = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    var userProfile = await _dbContext.UserProfiles
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.UserId == userId);
                    userEmail = userProfile?.Email;
                }
            }

            ViewBag.UserEmail = userEmail;

            var model = new RoomDto
            {
                Id = room.Id,
                RoomCode = room.RoomCode,
                RoomName = room.RoomName,
                RoomTypeId = room.RoomTypeId,
                RoomType = room.RoomType?.Name,
                PricePerNight = room.PricePerNight,
                MaxPeople = room.MaxPeople,
                Status = room.Status,
                Description = room.Description,
                ImageUrl = room.ImageUrl,
                IsActive = room.IsActive
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetServices()
        {
            try
            {
                var servicesForView = await _dbContext.Services
                    .AsNoTracking()
                    .Where(s => s.IsActive)
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        description = s.Description,
                        unitPrice = s.UnitPrice,
                        unit = s.Unit,
                        serviceTypeName = s.ServiceType!.Name
                    })
                    .OrderBy(s => s.serviceTypeName)
                    .ThenBy(s => s.name)
                    .ToListAsync();

                return Json(servicesForView);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateBooking(CreateBookingDto model)
        {
            if (!ModelState.IsValid)
            {
                TempData["BookingError"] = "Vui lòng điền đầy đủ thông tin";
                return RedirectToAction("Details", new { id = model.RoomId });
            }

            // Validate dates
            if (model.CheckInDate < DateTime.Today)
            {
                TempData["BookingError"] = "Ngày nhận phòng phải từ hôm nay trở đi";
                return RedirectToAction("Details", new { id = model.RoomId });
            }

            if (model.CheckOutDate <= model.CheckInDate)
            {
                TempData["BookingError"] = "Ngày trả phòng phải sau ngày nhận phòng";
                return RedirectToAction("Details", new { id = model.RoomId });
            }

            // Check room exists and is available
            var room = await _dbContext.Rooms.FindAsync(model.RoomId);
            if (room == null || !room.IsActive || room.Status != "Available")
            {
                TempData["BookingError"] = "Phòng không khả dụng";
                return RedirectToAction("Details", new { id = model.RoomId });
            }

            // Check for booking conflicts
            var hasConflict = await _dbContext.BookingDetails
                .Include(bd => bd.Booking)
                .AnyAsync(bd => bd.RoomId == model.RoomId &&
                               bd.Booking.Status != "Cancelled" &&
                               bd.Booking.CheckInDate < model.CheckOutDate &&
                               bd.Booking.CheckOutDate > model.CheckInDate);

            if (hasConflict)
            {
                TempData["BookingError"] = "Phòng đã được đặt trong khoảng thời gian này. Vui lòng chọn ngày khác.";
                return RedirectToAction("Details", new { id = model.RoomId });
            }

            // Get user ID - ưu tiên UserId từ form, sau đó mới lấy từ claims
            int userId;
            
            if (model.UserId.HasValue && model.UserId.Value > 0)
            {
                // Sử dụng UserId từ form (hidden field)
                userId = model.UserId.Value;
                System.Diagnostics.Debug.WriteLine($"DEBUG: Using UserId from form = {userId}");
            }
            else
            {
                // Fallback: lấy từ claims như cũ
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var usernameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
                
                System.Diagnostics.Debug.WriteLine($"DEBUG: UserIdClaim = {userIdClaim}");
                System.Diagnostics.Debug.WriteLine($"DEBUG: UsernameClaim = {usernameClaim}");
                System.Diagnostics.Debug.WriteLine($"DEBUG: All Claims = {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
                
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out userId))
                {
                    TempData["BookingError"] = $"Không thể xác định người dùng. UserIdClaim = {userIdClaim}";
                    return RedirectToAction("Details", new { id = model.RoomId });
                }
            }
            
            // Kiểm tra user có tồn tại trong database không
            var userExists = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (userExists == null)
            {
                TempData["BookingError"] = $"User ID {userId} không tồn tại hoặc đã bị vô hiệu hóa";
                return RedirectToAction("Details", new { id = model.RoomId });
            }
            
            System.Diagnostics.Debug.WriteLine($"DEBUG: Found user in DB: ID={userExists.Id}, Username={userExists.Username}");

            // Calculate booking details
            var totalNights = (model.CheckOutDate - model.CheckInDate).Days;
            var subtotal = room.PricePerNight * totalNights;

            // Calculate services total
            decimal servicesTotal = 0;
            List<BookingService> bookingServices = new List<BookingService>();

            if (model.ServiceIds != null && model.ServiceIds.Any())
            {
                var selectedServices = await _dbContext.Services
                    .Where(s => model.ServiceIds.Contains(s.Id) && s.IsActive)
                    .ToListAsync();

                foreach (var service in selectedServices)
                {
                    servicesTotal += service.UnitPrice;
                }
            }

            var subtotalWithServices = subtotal + servicesTotal;
            var vat = subtotalWithServices * 0.1m; // 10% VAT
            var totalAmount = subtotalWithServices + vat;

            // Generate booking code
            var bookingCode = $"BK{DateTime.Now:yyyyMMddHHmmss}";

            // Create booking
            var booking = new Booking
            {
                BookingCode = bookingCode,
                UserId = userId, // Sử dụng userId đã parse từ claim
                GuestName = model.GuestName,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                Address = model.Address,
                BookingDate = DateTime.Now,
                CheckInDate = model.CheckInDate,
                CheckOutDate = model.CheckOutDate,
                TotalNights = totalNights,
                SubTotal = subtotalWithServices,
                VAT = vat,
                TotalAmount = totalAmount,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };
            
            System.Diagnostics.Debug.WriteLine($"DEBUG: Creating booking with UserId = {booking.UserId}");

            _dbContext.Bookings.Add(booking);
            await _dbContext.SaveChangesAsync();
            
            System.Diagnostics.Debug.WriteLine($"DEBUG: Booking created with ID = {booking.Id}, UserId = {booking.UserId}");

            // Create booking detail
            var bookingDetail = new BookingDetail
            {
                BookingId = booking.Id,
                RoomId = model.RoomId,
                PricePerNight = room.PricePerNight
            };

            _dbContext.BookingDetails.Add(bookingDetail);

            // Create booking services
            if (model.ServiceIds != null && model.ServiceIds.Any())
            {
                var selectedServices = await _dbContext.Services
                    .Where(s => model.ServiceIds.Contains(s.Id) && s.IsActive)
                    .ToListAsync();

                foreach (var service in selectedServices)
                {
                    var bookingService = new BookingService
                    {
                        BookingId = booking.Id,
                        ServiceId = service.Id,
                        Quantity = 1,
                        UnitPrice = service.UnitPrice,
                        TotalPrice = service.UnitPrice
                    };
                    _dbContext.BookingServices.Add(bookingService);
                }
            }

            await _dbContext.SaveChangesAsync();

            TempData["BookingSuccess"] = $"Đặt phòng thành công! Mã đặt phòng: {bookingCode}";
            return RedirectToAction("BookingConfirmation", new { id = booking.Id });
        }

        [Authorize]
        public async Task<IActionResult> BookingConfirmation(int id)
        {
            var booking = await _dbContext.Bookings
                .Include(b => b.Details)
                .ThenInclude(bd => bd.Room)
                .Include(b => b.Services)
                .ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Tạm thời bỏ validation ownership để test - SẼ SỬA LẠI SAU
            // TODO: Sửa lại logic UserId khi đã fix được vấn đề claims
            /*
            // Check if user owns this booking - THÊM DEBUG INFO
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            System.Diagnostics.Debug.WriteLine($"DEBUG BookingConfirmation: BookingId={id}, Booking.UserId={booking.UserId}");
            System.Diagnostics.Debug.WriteLine($"DEBUG BookingConfirmation: UserIdClaim={userIdClaim}");
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                TempData["Error"] = $"Không thể xác định người dùng. UserIdClaim = {userIdClaim}";
                return RedirectToAction("Index", "Home", new { area = "Client" });
            }
            
            if (booking.UserId != userId)
            {
                TempData["Error"] = $"Bạn không có quyền xem booking này. Booking.UserId={booking.UserId}, Your.UserId={userId}";
                return RedirectToAction("Index", "Home", new { area = "Client" });
            }
            */

            return View(booking);
        }
    }
}
