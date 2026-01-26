using System.Security.Claims;
using System.Threading.Tasks;
using HotelManagement.Web.Data;
using HotelManagement.Web.DTOs;
using HotelManagement.Web.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginRequestDto());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginRequestDto model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username && (u.Role == "Admin" || u.Role == "Manager") && u.IsActive);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Đăng nhập thất bại. Kiểm tra lại thông tin.");
                return View(model);
            }

            var verified = VerifyAndMaybeRehash(user, model.Password);
            if (!verified)
            {
                ModelState.AddModelError(string.Empty, "Đăng nhập thất bại. Kiểm tra lại thông tin.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, "AdminAuth");
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync("AdminAuth", principal, new AuthenticationProperties
            {
                IsPersistent = true
            });

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("AdminAuth");
            return RedirectToAction("Login");
        }

        public IActionResult Denied()
        {
            return View();
        }

        private bool VerifyAndMaybeRehash(User user, string inputPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, inputPassword);
            if (result == PasswordVerificationResult.Success)
            {
                return true;
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, inputPassword);
                _dbContext.Users.Update(user);
                _dbContext.SaveChanges();
                return true;
            }

            // Hỗ trợ trường hợp mật khẩu đang lưu plain-text (legacy)
            if (user.PasswordHash == inputPassword)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, inputPassword);
                _dbContext.Users.Update(user);
                _dbContext.SaveChanges();
                return true;
            }

            return false;
        }
    }
}
