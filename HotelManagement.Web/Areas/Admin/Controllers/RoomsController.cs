using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HotelManagement.Web.Data;
using HotelManagement.Web.DTOs;
using HotelManagement.Web.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin,Manager")]
    public class RoomsController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;
        private readonly IWebHostEnvironment _env;

        public RoomsController(HotelManagementDbContext dbContext, IWebHostEnvironment env)
        {
            _dbContext = dbContext;
            _env = env;
        }

        public async Task<IActionResult> Index(string? searchName, int? roomTypeId, DateTime? checkInDate, DateTime? checkOutDate)
        {
            var query = _dbContext.Rooms.Include(r => r.RoomType).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(r => r.RoomName!.Contains(searchName) || r.RoomCode.Contains(searchName));
            }

            if (roomTypeId.HasValue && roomTypeId.Value > 0)
            {
                query = query.Where(r => r.RoomTypeId == roomTypeId.Value);
            }

            var rooms = await query
                .OrderBy(r => r.RoomCode)
                .ToListAsync();

            var currentDate = DateTime.Now.Date;
            
            // Chỉ tính toán trạng thái động khi có filter ngày
            if (checkInDate.HasValue && checkOutDate.HasValue)
            {
                // Lấy tất cả booking hiện tại để tính trạng thái phòng
                var currentBookings = await _dbContext.BookingDetails
                    .Include(bd => bd.Booking)
                    .ThenInclude(b => b!.User)
                    .ThenInclude(u => u!.Profile)
                    .Where(bd => bd.Booking != null && 
                                bd.Booking.Status == "Confirmed")
                    .ToListAsync();

                var roomDtos = rooms.Select(r => {
                    // Tính toán trạng thái phòng dựa trên booking hiện tại
                    var roomBookings = currentBookings.Where(bd => bd.RoomId == r.Id).ToList();
                    string calculatedStatus = CalculateRoomStatus(r, roomBookings, currentDate);

                    return new RoomDto
                    {
                        Id = r.Id,
                        RoomCode = r.RoomCode,
                        RoomName = r.RoomName,
                        RoomTypeId = r.RoomTypeId,
                        RoomType = r.RoomType != null ? r.RoomType.Name : "",
                        PricePerNight = r.PricePerNight,
                        MaxPeople = r.MaxPeople,
                        Status = calculatedStatus, // Sử dụng trạng thái được tính toán
                        Description = r.Description,
                        ImageUrl = r.ImageUrl,
                        IsActive = r.IsActive
                    };
                }).ToList();

                // Lấy danh sách booking trùng với khoảng thời gian
                var roomIds = rooms.Select(r => r.Id).ToList();
                var bookingDetails = await _dbContext.BookingDetails
                    .Include(bd => bd.Booking)
                    .ThenInclude(b => b!.User)
                    .ThenInclude(u => u!.Profile)
                    .Where(bd => roomIds.Contains(bd.RoomId) &&
                                 bd.Booking != null &&
                                 bd.Booking.Status == "Confirmed" &&
                                 bd.Booking.CheckInDate < checkOutDate.Value &&
                                 bd.Booking.CheckOutDate > checkInDate.Value)
                    .ToListAsync();

                var bookings = bookingDetails.Select(bd => new
                {
                    RoomId = bd.RoomId,
                    CustomerName = bd.Booking!.User?.Profile?.FullName ?? bd.Booking.User?.Username ?? "N/A",
                    CheckInDate = bd.Booking.CheckInDate,
                    CheckOutDate = bd.Booking.CheckOutDate,
                    Status = bd.Booking.Status
                }).ToList();
                
                ViewBag.RoomBookings = bookings.GroupBy(b => b.RoomId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Truyền roomDtos đã tính toán trạng thái
                ViewBag.RoomDtos = roomDtos;
            }
            else
            {
                // Không filter ngày - sử dụng trạng thái tĩnh từ database
                var roomDtos = rooms.Select(r => new RoomDto
                {
                    Id = r.Id,
                    RoomCode = r.RoomCode,
                    RoomName = r.RoomName,
                    RoomTypeId = r.RoomTypeId,
                    RoomType = r.RoomType != null ? r.RoomType.Name : "",
                    PricePerNight = r.PricePerNight,
                    MaxPeople = r.MaxPeople,
                    Status = r.Status, // Sử dụng trạng thái tĩnh từ database
                    Description = r.Description,
                    ImageUrl = r.ImageUrl,
                    IsActive = r.IsActive
                }).ToList();

                ViewBag.RoomBookings = new Dictionary<int, List<object>>();
                ViewBag.RoomDtos = roomDtos;
            }

            ViewBag.RoomTypes = await _dbContext.RoomTypes
                .Where(rt => rt.IsActive)
                .Select(rt => new SelectListItem { Value = rt.Id.ToString(), Text = rt.Name })
                .ToListAsync();

            ViewBag.SearchName = searchName;
            ViewBag.RoomTypeId = roomTypeId;
            ViewBag.CheckInDate = checkInDate?.ToString("yyyy-MM-dd");
            ViewBag.CheckOutDate = checkOutDate?.ToString("yyyy-MM-dd");

            return View(ViewBag.RoomDtos);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadRoomTypesList();
            return View(new RoomDto { IsActive = true, Status = "Available" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomDto model)
        {
            if (!ModelState.IsValid)
            {
                await LoadRoomTypesList();
                return View(model);
            }

            var exists = await _dbContext.Rooms.AnyAsync(r => r.RoomCode == model.RoomCode.Trim());
            if (exists)
            {
                ModelState.AddModelError("RoomCode", "Mã phòng đã tồn tại.");
                await LoadRoomTypesList();
                return View(model);
            }

            var room = new Room
            {
                RoomCode = model.RoomCode.Trim(),
                RoomName = model.RoomName?.Trim(),
                RoomTypeId = model.RoomTypeId,
                PricePerNight = model.PricePerNight,
                MaxPeople = model.MaxPeople,
                Status = model.Status?.Trim() ?? "Available",
                Description = model.Description?.Trim(),
                ImageUrl = model.ImageUrl?.Trim(),
                IsActive = model.IsActive
            };

            // Save uploaded image if available
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                room.ImageUrl = await SaveRoomImage(model.ImageFile);
            }

            _dbContext.Rooms.Add(room);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Thêm phòng thành công.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _dbContext.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            var model = new RoomDto
            {
                Id = room.Id,
                RoomCode = room.RoomCode,
                RoomName = room.RoomName,
                RoomTypeId = room.RoomTypeId,
                PricePerNight = room.PricePerNight,
                MaxPeople = room.MaxPeople,
                Status = room.Status,
                Description = room.Description,
                ImageUrl = room.ImageUrl,
                IsActive = room.IsActive
            };

            await LoadRoomTypesList();
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoomDto model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await LoadRoomTypesList();
                return View(model);
            }

            var room = await _dbContext.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            var exists = await _dbContext.Rooms.AnyAsync(r => r.RoomCode == model.RoomCode.Trim() && r.Id != id);
            if (exists)
            {
                ModelState.AddModelError("RoomCode", "Mã phòng đã tồn tại.");
                await LoadRoomTypesList();
                return View(model);
            }

            room.RoomCode = model.RoomCode.Trim();
            room.RoomName = model.RoomName?.Trim();
            room.RoomTypeId = model.RoomTypeId;
            room.PricePerNight = model.PricePerNight;
            room.MaxPeople = model.MaxPeople;
            room.Status = model.Status?.Trim() ?? "Available";
            room.Description = model.Description?.Trim();
            room.IsActive = model.IsActive;

            // Only update ImageUrl if a new file is uploaded
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                room.ImageUrl = await SaveRoomImage(model.ImageFile);
            }
            else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
            {
                // Keep existing ImageUrl if no new file uploaded
                room.ImageUrl = model.ImageUrl.Trim();
            }

            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Cập nhật phòng thành công.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _dbContext.Rooms
                .Include(r => r.BookingDetails)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return NotFound();
            }

            if (room.BookingDetails.Any())
            {
                TempData["Error"] = "Không thể xóa phòng đã có booking.";
                return RedirectToAction(nameof(Index));
            }

            _dbContext.Rooms.Remove(room);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Xóa phòng thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var room = await _dbContext.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return NotFound();
            }

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

        // Action để cập nhật trạng thái phòng
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoomStatus(int id, string status)
        {
            var room = await _dbContext.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            if (status == "Available" && room.Status == "Bảo trì")
            {
                room.Status = "Available";
                await _dbContext.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật trạng thái phòng {room.RoomCode} thành 'Khả dụng'.";
            }
            else if (status == "Còn trống" && room.Status == "Bảo trì")
            {
                room.Status = "Còn trống";
                await _dbContext.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật trạng thái phòng {room.RoomCode} thành 'Còn trống'.";
            }
            else
            {
                TempData["Error"] = "Không thể cập nhật trạng thái phòng.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadRoomTypesList()
        {
            ViewBag.RoomTypes = await _dbContext.RoomTypes
                .Where(rt => rt.IsActive)
                .Select(rt => new SelectListItem { Value = rt.Id.ToString(), Text = rt.Name })
                .ToListAsync();
        }

        private async Task<string> SaveRoomImage(Microsoft.AspNetCore.Http.IFormFile file)
        {
            try
            {
                // Use same path as Blog module: C:\img_hotel
                var uploadsPath = @"C:\img_hotel";
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Verify file was saved
                if (!System.IO.File.Exists(filePath))
                {
                    throw new Exception($"File save failed: {filePath}");
                }

                // Return full path like Blog does (ImageController will handle it)
                return filePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving room image: {ex.Message}", ex);
            }
        }

        private string GetMimeType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }

        // Cập nhật phương thức tính toán trạng thái phòng theo yêu cầu mới
        private string CalculateRoomStatus(Room room, List<HotelManagement.Web.Entities.BookingDetail> bookings, DateTime currentDate)
        {
            if (!room.IsActive)
            {
                return "Không khả dụng";
            }

            // Kiểm tra nếu phòng đang ở trạng thái bảo trì (được set thủ công)
            if (room.Status == "Bảo trì")
            {
                return "Bảo trì";
            }

            // Kiểm tra booking hiện tại
            foreach (var booking in bookings)
            {
                if (booking.Booking == null) continue;

                var checkIn = booking.Booking.CheckInDate.Date;
                var checkOut = booking.Booking.CheckOutDate.Date;

                if (booking.Booking.Status == "Confirmed")
                {
                    // Nếu ngày hiện tại >= check-in và < check-out → Đang sử dụng
                    if (currentDate >= checkIn && currentDate < checkOut)
                    {
                        return "Đang sử dụng";
                    }
                    // Nếu ngày hiện tại >= check-out → Bảo trì (tự động)
                    else if (currentDate >= checkOut)
                    {
                        // Cập nhật trạng thái phòng trong database thành bảo trì
                        room.Status = "Bảo trì";
                        _dbContext.SaveChanges();
                        return "Bảo trì";
                    }
                    // Nếu ngày hiện tại < check-in → Đã đặt
                    else if (currentDate < checkIn)
                    {
                        return "Đã đặt";
                    }
                }
            }

            // Nếu không có booking nào hoặc booking đã hủy → Còn trống
            return "Còn trống";
        }
    }
}
