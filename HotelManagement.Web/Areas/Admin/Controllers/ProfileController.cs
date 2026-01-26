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
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin,Manager")]
    public class ProfileController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public ProfileController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var user = await _dbContext.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileUpdateDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                FullName = user.Profile?.FullName,
                Email = user.Profile?.Email,
                PhoneNumber = user.Profile?.PhoneNumber,
                Address = user.Profile?.Address,
                DateOfBirth = user.Profile?.DateOfBirth,
                Gender = user.Profile?.Gender
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(ProfileUpdateDto model)
        {
            var userId = GetCurrentUserId();
            var user = await _dbContext.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Cập nhật thông tin cá nhân
            if (user.Profile == null)
            {
                user.Profile = new UserProfile { UserId = user.Id };
            }

            user.Profile.FullName = model.FullName?.Trim();
            user.Profile.Email = model.Email?.Trim();
            user.Profile.PhoneNumber = model.PhoneNumber?.Trim();
            user.Profile.Address = model.Address?.Trim();
            user.Profile.DateOfBirth = model.DateOfBirth;
            user.Profile.Gender = model.Gender?.Trim();

            // Đổi mật khẩu (nếu có nhập)
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Vui lòng nhập mật khẩu hiện tại.");
                    return View(model);
                }

                var verified = VerifyPassword(user, model.CurrentPassword, out var needsRehashCurrent);
                if (!verified)
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                    return View(model);
                }

                user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
            }

            await _dbContext.SaveChangesAsync();
            TempData["Success"] = "Cập nhật hồ sơ thành công.";
            return RedirectToAction("Index");
        }

        private bool VerifyPassword(User user, string inputPassword, out bool needsRehash)
        {
            needsRehash = false;
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return false;
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, inputPassword);
            if (result == PasswordVerificationResult.Success)
            {
                return true;
            }
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                needsRehash = true;
                return true;
            }

            // Hỗ trợ trường hợp mật khẩu đang lưu ở dạng plain-text (legacy)
            if (user.PasswordHash == inputPassword)
            {
                needsRehash = true;
                return true;
            }

            return false;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!);
        }
    }
}
