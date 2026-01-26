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
    public class ServiceTypesController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;

        public ServiceTypesController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var serviceTypes = await _dbContext.ServiceTypes
                .OrderBy(st => st.Name)
                .Select(st => new ServiceTypeDto
                {
                    Id = st.Id,
                    Name = st.Name
                })
                .ToListAsync();

            return View(serviceTypes);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new ServiceTypeDto());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceTypeDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var exists = await _dbContext.ServiceTypes.AnyAsync(st => st.Name == model.Name.Trim());
            if (exists)
            {
                ModelState.AddModelError("Name", "Tên loại dịch vụ đã tồn tại.");
                return View(model);
            }

            var serviceType = new ServiceType
            {
                Name = model.Name.Trim()
            };

            _dbContext.ServiceTypes.Add(serviceType);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Thêm loại dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var serviceType = await _dbContext.ServiceTypes.FindAsync(id);
            if (serviceType == null)
            {
                return NotFound();
            }

            var model = new ServiceTypeDto
            {
                Id = serviceType.Id,
                Name = serviceType.Name
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServiceTypeDto model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var serviceType = await _dbContext.ServiceTypes.FindAsync(id);
            if (serviceType == null)
            {
                return NotFound();
            }

            var exists = await _dbContext.ServiceTypes.AnyAsync(st => st.Name == model.Name.Trim() && st.Id != id);
            if (exists)
            {
                ModelState.AddModelError("Name", "Tên loại dịch vụ đã tồn tại.");
                return View(model);
            }

            serviceType.Name = model.Name.Trim();
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Cập nhật loại dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var serviceType = await _dbContext.ServiceTypes
                .Include(st => st.Services)
                .FirstOrDefaultAsync(st => st.Id == id);

            if (serviceType == null)
            {
                return NotFound();
            }

            if (serviceType.Services.Any())
            {
                TempData["Error"] = "Không thể xóa loại dịch vụ đang có dịch vụ sử dụng.";
                return RedirectToAction(nameof(Index));
            }

            _dbContext.ServiceTypes.Remove(serviceType);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Xóa loại dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
