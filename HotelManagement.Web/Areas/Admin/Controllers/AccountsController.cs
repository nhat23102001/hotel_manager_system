using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HotelManagement.Web.Data;
using HotelManagement.Web.DTOs;
using HotelManagement.Web.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class AccountsController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AccountsController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(string? searchName, string? searchEmail)
        {
            var query = _dbContext.Users.Include(u => u.Profile).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(u => u.Username.Contains(searchName) || (u.Profile != null && u.Profile.FullName != null && u.Profile.FullName.Contains(searchName)));
            }

            if (!string.IsNullOrWhiteSpace(searchEmail))
            {
                query = query.Where(u => u.Profile != null && u.Profile.Email != null && u.Profile.Email.Contains(searchEmail));
            }

            var users = await query
                .OrderBy(u => u.Username)
                .Select(u => new AccountManagementDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Role = u.Role,
                    FullName = u.Profile != null ? u.Profile.FullName : null,
                    Email = u.Profile != null ? u.Profile.Email : null,
                    PhoneNumber = u.Profile != null ? u.Profile.PhoneNumber : null,
                    IsActive = u.IsActive
                })
                .ToListAsync();

            ViewBag.SearchName = searchName;
            ViewBag.SearchEmail = searchEmail;

            return View(users);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new AccountManagementDto { IsActive = true, Role = "Client" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AccountManagementDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu.");
                return View(model);
            }

            var exists = await _dbContext.Users.AnyAsync(u => u.Username == model.Username.Trim());
            if (exists)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            var user = new User
            {
                Username = model.Username.Trim(),
                PasswordHash = string.Empty,
                Role = model.Role,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            user.Profile = new UserProfile
            {
                FullName = model.FullName?.Trim(),
                Email = model.Email?.Trim(),
                PhoneNumber = model.PhoneNumber?.Trim(),
                Address = model.Address?.Trim()
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Thêm tài khoản thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _dbContext.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var model = new AccountManagementDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                FullName = user.Profile?.FullName,
                Email = user.Profile?.Email,
                PhoneNumber = user.Profile?.PhoneNumber,
                Address = user.Profile?.Address,
                IsActive = user.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AccountManagementDto model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _dbContext.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var exists = await _dbContext.Users.AnyAsync(u => u.Username == model.Username.Trim() && u.Id != id);
            if (exists)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            user.Username = model.Username.Trim();
            user.Role = model.Role;
            user.IsActive = model.IsActive;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
            }

            if (user.Profile == null)
            {
                user.Profile = new UserProfile { UserId = user.Id };
            }

            user.Profile.FullName = model.FullName?.Trim();
            user.Profile.Email = model.Email?.Trim();
            user.Profile.PhoneNumber = model.PhoneNumber?.Trim();
            user.Profile.Address = model.Address?.Trim();

            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Cập nhật tài khoản thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _dbContext.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var model = new AccountManagementDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                FullName = user.Profile?.FullName,
                Email = user.Profile?.Email,
                PhoneNumber = user.Profile?.PhoneNumber,
                Address = user.Profile?.Address,
                IsActive = user.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (id == currentUserId)
            {
                TempData["Error"] = "Không thể xóa tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _dbContext.Users
                .Include(u => u.Bookings)
                .Include(u => u.Blogs)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Bookings.Any() || user.Blogs.Any())
            {
                TempData["Error"] = "Không thể xóa tài khoản đã có dữ liệu liên quan.";
                return RedirectToAction(nameof(Index));
            }

            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Xóa tài khoản thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
