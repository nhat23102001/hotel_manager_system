using HotelManagement.Web.Data;
using HotelManagement.Web.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Areas.Client.Controllers
{
    [Area("Client")]
    public class ServicesController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;

        public ServicesController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(int? serviceTypeId, string? search, int page = 1)
        {
            int pageSize = 9;
            var query = _dbContext.Services
                .Include(s => s.ServiceType)
                .Where(s => s.IsActive)
                .AsQueryable();

            // Filter by search
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s => s.Name.Contains(search) ||
                                       (s.Description != null && s.Description.Contains(search)));
            }

            // Filter by service type
            if (serviceTypeId.HasValue && serviceTypeId.Value > 0)
            {
                query = query.Where(s => s.ServiceTypeId == serviceTypeId.Value);
            }

            // Count total services
            var totalServices = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalServices / pageSize);

            // Validate page number
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var services = await query
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new ServiceDto
                {
                    Id = s.Id,
                    ServiceTypeId = s.ServiceTypeId,
                    Name = s.Name,
                    Description = s.Description,
                    UnitPrice = s.UnitPrice,
                    Unit = s.Unit,
                    IsActive = s.IsActive,
                    ServiceTypeName = s.ServiceType != null ? s.ServiceType.Name : ""
                })
                .ToListAsync();

            // Load service types for filter dropdown
            ViewBag.ServiceTypes = await _dbContext.ServiceTypes
                .Select(st => new SelectListItem { Value = st.Id.ToString(), Text = st.Name })
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.ServiceTypeId = serviceTypeId;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalServices = totalServices;

            return View(services);
        }

        public async Task<IActionResult> Details(int id)
        {
            var service = await _dbContext.Services
                .Include(s => s.ServiceType)
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

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
                IsActive = service.IsActive,
                ServiceTypeName = service.ServiceType?.Name
            };

            return View(model);
        }
    }
}
