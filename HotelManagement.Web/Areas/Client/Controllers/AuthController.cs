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

namespace HotelManagement.Web.Areas.Client.Controllers
{
    [Area("Client")]
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;
        private readonly PasswordHasher<User> _passwordHasher = new();
        private readonly Services.IEmailSender _emailSender;

        public AuthController(HotelManagementDbContext dbContext, Services.IEmailSender emailSender)
        {
            _dbContext = dbContext;
            _emailSender = emailSender;
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username && u.Role == "Client" && u.IsActive);

            if (user == null)
            {
                TempData["LoginError"] = "Sai tài khoản hoặc mật khẩu.";
                return View(model);
            }

            var verified = VerifyAndMaybeRehash(user, model.Password);
            if (!verified)
            {
                TempData["LoginError"] = "Sai tài khoản hoặc mật khẩu.";
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            
            // Sign in with both default and ClientAuth schemes
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });
            
            await HttpContext.SignInAsync("ClientAuth", principal, new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect("/Client");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterRequestDto());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp.");
                return View(model);
            }

            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            var newUser = new User
            {
                Username = model.Username,
                Role = "Client",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, model.Password);

            _dbContext.Users.Add(newUser);
            await _dbContext.SaveChangesAsync();

            TempData["RegisterSuccess"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập và cập nhật thông tin cá nhân (bắt buộc nhập email).";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordDto());
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find user by email in UserProfile
            var userProfile = await _dbContext.UserProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Email == model.Email && p.User != null && p.User.Role == "Client");

            if (userProfile == null || userProfile.User == null)
            {
                // Don't reveal whether email exists
                TempData["ForgotSuccess"] = "Nếu email tồn tại trong hệ thống, mật khẩu mới sẽ được gửi đến email của bạn.";
                return View(model);
            }

            var user = userProfile.User;
            var newPassword = "123456";
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();

            // Send email with new password
            try
            {
                var subject = "Rolax Hotel - Khôi phục mật khẩu";
                var body = $@"Xin chào {userProfile.FullName ?? user.Username},

Bạn đã yêu cầu khôi phục mật khẩu tài khoản tại Rolax Hotel.

Mật khẩu mới của bạn là: {newPassword}

Vui lòng đăng nhập và thay đổi mật khẩu sau khi đăng nhập thành công.

Thông tin tài khoản:
- Tên đăng nhập: {user.Username}
- Mật khẩu: {newPassword}

Nếu bạn không yêu cầu khôi phục mật khẩu, vui lòng liên hệ với chúng tôi ngay.

Trân trọng,
Rolax Hotel
Email: rolaxhotel@gmail.com
Phone: +84 (28) 3914 5456";

                await _emailSender.SendEmailAsync(model.Email, subject, body);
                TempData["ForgotSuccess"] = "Mật khẩu mới đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư.";
            }
            catch (Exception)
            {
                // If email fails, still show success but mention to contact support
                TempData["ForgotSuccess"] = "Mật khẩu đã được đặt lại thành '123456'. Tuy nhiên, không thể gửi email. Vui lòng đăng nhập với mật khẩu mới.";
            }

            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync("ClientAuth");
            return RedirectToAction("Login");
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

            if (user.PasswordHash == inputPassword)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, inputPassword);
                _dbContext.Users.Update(user);
                _dbContext.SaveChanges();
                return true;
            }

            return false;
        }

        [HttpGet]
        public IActionResult CheckAuth()
        {
            var isAuth = User?.Identity?.IsAuthenticated ?? false;
            var userName = User?.Identity?.Name ?? "Not logged in";
            var claims = User?.Claims?.Select(c => new { c.Type, c.Value }).ToList();
            
            return Json(new 
            { 
                isAuthenticated = isAuth,
                userName = userName,
                claims = claims,
                cookieExists = Request.Cookies.ContainsKey("HotelClientAuth")
            });
        }
    }
}
