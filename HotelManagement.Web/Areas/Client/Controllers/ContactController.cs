using System;
using System.Threading.Tasks;
using HotelManagement.Web.Data;
using HotelManagement.Web.DTOs;
using HotelManagement.Web.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Areas.Client.Controllers
{
    [Area("Client")]
    public class ContactController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;

        public ContactController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ContactDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var contact = new Contact
            {
                Name = model.Name.Trim(),
                Email = model.Email.Trim(),
                Message = model.Message.Trim(),
                Status = "New",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Contacts.Add(contact);
            await _dbContext.SaveChangesAsync();

            TempData["ContactSuccess"] = "Cảm ơn bạn đã liên hệ. Chúng tôi sẽ phản hồi trong thời gian sớm nhất.";
            return RedirectToAction(nameof(Index));
        }
    }
}
