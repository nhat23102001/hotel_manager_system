using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelManagement.Web.Data;
using HotelManagement.Web.DTOs;
using HotelManagement.Web.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin,Manager")]
    public class RoomTypesController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;

        public RoomTypesController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var roomTypes = await _dbContext.RoomTypes
                .OrderBy(rt => rt.Name)
                .Select(rt => new RoomTypeDto
                {
                    Id = rt.Id,
                    Name = rt.Name,
                    Description = rt.Description,
                    BasePrice = rt.BasePrice,
                    MaxPeople = rt.MaxPeople,
                    IsActive = rt.IsActive
                })
                .ToListAsync();

            return View(roomTypes);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new RoomTypeDto());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomTypeDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var roomType = new RoomType
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                BasePrice = model.BasePrice,
                MaxPeople = model.MaxPeople,
                IsActive = model.IsActive
            };

            _dbContext.RoomTypes.Add(roomType);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Thêm loại phòng thành công.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var roomType = await _dbContext.RoomTypes.FindAsync(id);
            if (roomType == null)
            {
                return NotFound();
            }

            var model = new RoomTypeDto
            {
                Id = roomType.Id,
                Name = roomType.Name,
                Description = roomType.Description,
                BasePrice = roomType.BasePrice,
                MaxPeople = roomType.MaxPeople,
                IsActive = roomType.IsActive
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoomTypeDto model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var roomType = await _dbContext.RoomTypes.FindAsync(id);
            if (roomType == null)
            {
                return NotFound();
            }

            roomType.Name = model.Name.Trim();
            roomType.Description = model.Description?.Trim();
            roomType.BasePrice = model.BasePrice;
            roomType.MaxPeople = model.MaxPeople;
            roomType.IsActive = model.IsActive;

            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Cập nhật loại phòng thành công.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var roomType = await _dbContext.RoomTypes
                .Include(rt => rt.Rooms)
                .FirstOrDefaultAsync(rt => rt.Id == id);

            if (roomType == null)
            {
                return NotFound();
            }

            if (roomType.Rooms.Any())
            {
                TempData["Error"] = "Không thể xóa loại phòng đang có phòng sử dụng.";
                return RedirectToAction(nameof(Index));
            }

            _dbContext.RoomTypes.Remove(roomType);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Xóa loại phòng thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
