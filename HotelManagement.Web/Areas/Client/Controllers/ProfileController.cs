using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HotelManagement.Web.Data;
using HotelManagement.Web.DTOs;
using HotelManagement.Web.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize(AuthenticationSchemes = "ClientAuth", Roles = "Client")]
    public class ProfileController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;

        public ProfileController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var user = await _dbContext.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileUpdateDto
            {
                FullName = user.Profile?.FullName,
                PhoneNumber = user.Profile?.PhoneNumber,
                Email = user.Profile?.Email,
                Address = user.Profile?.Address,
                DateOfBirth = user.Profile?.DateOfBirth,
                Gender = user.Profile?.Gender
            };

            ViewBag.Username = user.Username;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileUpdateDto model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                var user2 = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                ViewBag.Username = user2?.Username;
                return View(model);
            }

            // Validate email is required
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError("Email", "Email là bắt buộc.");
                var user2 = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                ViewBag.Username = user2?.Username;
                return View(model);
            }

            var user = await _dbContext.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Profile == null)
            {
                user.Profile = new UserProfile
                {
                    UserId = userId
                };
                _dbContext.UserProfiles.Add(user.Profile);
            }

            user.Profile.FullName = model.FullName;
            user.Profile.PhoneNumber = model.PhoneNumber;
            user.Profile.Email = model.Email;
            user.Profile.Address = model.Address;
            user.Profile.DateOfBirth = model.DateOfBirth;
            user.Profile.Gender = model.Gender;

            await _dbContext.SaveChangesAsync();

            TempData["ProfileSuccess"] = "Cập nhật thông tin cá nhân thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
