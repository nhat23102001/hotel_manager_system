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

namespace HotelManagement.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin,Manager")]
    public class ContactsController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;

        public ContactsController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(string? status)
        {
            var query = _dbContext.Contacts
                .Include(c => c.RepliedByUser)
                    .ThenInclude(u => u.Profile)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            var contacts = await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ContactDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email,
                    Message = c.Message,
                    Status = c.Status,
                    ReplyContent = c.ReplyContent,
                    RepliedAt = c.RepliedAt,
                    CreatedAt = c.CreatedAt,
                    RepliedByName = c.RepliedByUser != null
                        ? (c.RepliedByUser.Profile != null && !string.IsNullOrWhiteSpace(c.RepliedByUser.Profile.FullName)
                            ? c.RepliedByUser.Profile.FullName
                            : c.RepliedByUser.Username)
                        : null
                })
                .ToListAsync();

            ViewBag.Status = status;
            return View(contacts);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var contact = await _dbContext.Contacts
                .Include(c => c.RepliedByUser)
                    .ThenInclude(u => u.Profile)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contact == null)
            {
                return NotFound();
            }

            var model = new ContactDto
            {
                Id = contact.Id,
                Name = contact.Name,
                Email = contact.Email,
                Message = contact.Message,
                Status = contact.Status,
                ReplyContent = contact.ReplyContent,
                RepliedAt = contact.RepliedAt,
                RepliedByName = contact.RepliedByUser != null
                    ? (contact.RepliedByUser.Profile != null && !string.IsNullOrWhiteSpace(contact.RepliedByUser.Profile.FullName)
                        ? contact.RepliedByUser.Profile.FullName
                        : contact.RepliedByUser.Username)
                    : null,
                CreatedAt = contact.CreatedAt
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string replyContent, string? status)
        {
            var contact = await _dbContext.Contacts.FindAsync(id);
            if (contact == null)
            {
                return NotFound();
            }

            contact.ReplyContent = replyContent?.Trim();
            contact.Status = string.IsNullOrWhiteSpace(status) ? "Replied" : status;
            contact.RepliedBy = GetCurrentUserId();
            contact.RepliedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Đã phản hồi liên hệ.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var contact = await _dbContext.Contacts.FindAsync(id);
            if (contact == null)
            {
                return NotFound();
            }

            _dbContext.Contacts.Remove(contact);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Xóa liên hệ thành công.";
            return RedirectToAction(nameof(Index));
        }

        private int? GetCurrentUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idStr, out var id) ? id : null;
        }
    }
}
