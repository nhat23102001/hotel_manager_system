using System.Linq;
using System.Threading.Tasks;
using HotelManagement.Web.Data;
using HotelManagement.Web.DTOs;
using HotelManagement.Web.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin,Manager")]
    public class ServicesController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;

        public ServicesController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(string? searchName, int? serviceTypeId)
        {
            var query = _dbContext.Services.Include(s => s.ServiceType).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(s => s.Name.Contains(searchName));
            }

            if (serviceTypeId.HasValue && serviceTypeId.Value > 0)
            {
                query = query.Where(s => s.ServiceTypeId == serviceTypeId.Value);
            }

            var services = await query
                .OrderBy(s => s.Name)
                .Select(s => new ServiceDto
                {
                    Id = s.Id,
                    ServiceTypeId = s.ServiceTypeId,
                    ServiceTypeName = s.ServiceType != null ? s.ServiceType.Name : string.Empty,
                    Name = s.Name,
                    Description = s.Description,
                    UnitPrice = s.UnitPrice,
                    Unit = s.Unit,
                    IsActive = s.IsActive
                })
                .ToListAsync();

            await LoadServiceTypesList();
            ViewBag.SearchName = searchName;
            ViewBag.ServiceTypeId = serviceTypeId;

            return View(services);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadServiceTypesList();
            return View(new ServiceDto { IsActive = true });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceDto model)
        {
            if (!ModelState.IsValid)
            {
                await LoadServiceTypesList();
                return View(model);
            }

            var exists = await _dbContext.Services.AnyAsync(s => s.Name == model.Name.Trim());
            if (exists)
            {
                ModelState.AddModelError("Name", "Tên dịch vụ đã tồn tại.");
                await LoadServiceTypesList();
                return View(model);
            }

            var service = new Service
            {
                ServiceTypeId = model.ServiceTypeId,
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                UnitPrice = model.UnitPrice,
                Unit = model.Unit?.Trim(),
                IsActive = model.IsActive
            };

            _dbContext.Services.Add(service);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Thêm dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var service = await _dbContext.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            var model = new ServiceDto
            {
                Id = service.Id,
                ServiceTypeId = service.ServiceTypeId,
                Name = service.Name,
                Description = service.Description,
                UnitPrice = service.UnitPrice,
                Unit = service.Unit,
                IsActive = service.IsActive
            };

            await LoadServiceTypesList();
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServiceDto model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await LoadServiceTypesList();
                return View(model);
            }

            var service = await _dbContext.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            var exists = await _dbContext.Services.AnyAsync(s => s.Name == model.Name.Trim() && s.Id != id);
            if (exists)
            {
                ModelState.AddModelError("Name", "Tên dịch vụ đã tồn tại.");
                await LoadServiceTypesList();
                return View(model);
            }

            service.ServiceTypeId = model.ServiceTypeId;
            service.Name = model.Name.Trim();
            service.Description = model.Description?.Trim();
            service.UnitPrice = model.UnitPrice;
            service.Unit = model.Unit?.Trim();
            service.IsActive = model.IsActive;

            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Cập nhật dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _dbContext.Services
                .Include(s => s.BookingServices)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
            {
                return NotFound();
            }

            if (service.BookingServices.Any())
            {
                TempData["Error"] = "Không thể xóa dịch vụ đang được sử dụng trong booking.";
                return RedirectToAction(nameof(Index));
            }

            _dbContext.Services.Remove(service);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Xóa dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var service = await _dbContext.Services
                .Include(s => s.ServiceType)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
            {
                return NotFound();
            }

            var model = new ServiceDto
            {
                Id = service.Id,
                ServiceTypeId = service.ServiceTypeId,
                ServiceTypeName = service.ServiceType?.Name,
                Name = service.Name,
                Description = service.Description,
                UnitPrice = service.UnitPrice,
                Unit = service.Unit,
                IsActive = service.IsActive
            };

            return View(model);
        }

        private async Task LoadServiceTypesList()
        {
            ViewBag.ServiceTypes = await _dbContext.ServiceTypes
                .OrderBy(st => st.Name)
                .Select(st => new SelectListItem { Value = st.Id.ToString(), Text = st.Name })
                .ToListAsync();
        }
    }
}
