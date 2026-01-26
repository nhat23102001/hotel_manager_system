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
    public class BlogsController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;

        public BlogsController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(string? search, bool? isPublished)
        {
            var query = _dbContext.Blogs
                .Include(b => b.Author)
                    .ThenInclude(a => a.Profile)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b => b.Title.Contains(search) || (b.Summary != null && b.Summary.Contains(search)));
            }

            if (isPublished.HasValue)
            {
                query = query.Where(b => b.IsPublished == isPublished.Value);
            }

            var blogs = await query
                .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
                .Select(b => new BlogDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Slug = b.Slug,
                    Summary = b.Summary,
                    Thumbnail = b.Thumbnail,
                    IsPublished = b.IsPublished,
                    PublishedAt = b.PublishedAt,
                    CreatedAt = b.CreatedAt,
                    AuthorName = b.Author != null
                        ? (b.Author.Profile != null && !string.IsNullOrWhiteSpace(b.Author.Profile.FullName)
                            ? b.Author.Profile.FullName
                            : b.Author.Username)
                        : null
                })
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.IsPublished = isPublished;

            return View(blogs);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var blog = await _dbContext.Blogs
                .Include(b => b.Author)
                    .ThenInclude(a => a.Profile)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (blog == null)
            {
                return NotFound();
            }

            var model = new BlogDto
            {
                Id = blog.Id,
                Title = blog.Title,
                Slug = blog.Slug,
                Summary = blog.Summary,
                Content = blog.Content,
                Thumbnail = blog.Thumbnail,
                IsPublished = blog.IsPublished,
                PublishedAt = blog.PublishedAt,
                CreatedAt = blog.CreatedAt,
                AuthorName = blog.Author != null
                    ? (blog.Author.Profile != null && !string.IsNullOrWhiteSpace(blog.Author.Profile.FullName)
                        ? blog.Author.Profile.FullName
                        : blog.Author.Username)
                    : null
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new BlogDto { IsPublished = true, PublishedAt = DateTime.UtcNow });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? thumbnailPath = null;
            if (model.ThumbnailFile != null && model.ThumbnailFile.Length > 0)
            {
                thumbnailPath = await SaveBlogImage(model.ThumbnailFile);
            }

            var blog = new Blog
            {
                Title = model.Title.Trim(),
                Slug = !string.IsNullOrWhiteSpace(model.Slug) ? model.Slug.Trim() : GenerateSlug(model.Title),
                Summary = model.Summary?.Trim(),
                Content = model.Content?.Trim(),
                Thumbnail = thumbnailPath ?? model.Thumbnail?.Trim(),
                IsPublished = model.IsPublished,
                PublishedAt = model.PublishedAt,
                CreatedAt = DateTime.UtcNow,
                AuthorId = GetCurrentUserId()
            };

            _dbContext.Blogs.Add(blog);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Thêm bài viết thành công.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var blog = await _dbContext.Blogs.FindAsync(id);
            if (blog == null)
            {
                return NotFound();
            }

            var model = new BlogDto
            {
                Id = blog.Id,
                Title = blog.Title,
                Slug = blog.Slug,
                Summary = blog.Summary,
                Content = blog.Content,
                Thumbnail = blog.Thumbnail,
                IsPublished = blog.IsPublished,
                PublishedAt = blog.PublishedAt,
                CreatedAt = blog.CreatedAt
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogDto model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var blog = await _dbContext.Blogs.FindAsync(id);
            if (blog == null)
            {
                return NotFound();
            }

            string? thumbnailPath = blog.Thumbnail;
            if (model.ThumbnailFile != null && model.ThumbnailFile.Length > 0)
            {
                thumbnailPath = await SaveBlogImage(model.ThumbnailFile);
            }
            else if (!string.IsNullOrWhiteSpace(model.Thumbnail))
            {
                thumbnailPath = model.Thumbnail;
            }

            blog.Title = model.Title.Trim();
            blog.Slug = !string.IsNullOrWhiteSpace(model.Slug) ? model.Slug.Trim() : GenerateSlug(model.Title);
            blog.Summary = model.Summary?.Trim();
            blog.Content = model.Content?.Trim();
            blog.Thumbnail = thumbnailPath;
            blog.IsPublished = model.IsPublished;
            blog.PublishedAt = model.PublishedAt;

            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Cập nhật bài viết thành công.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var blog = await _dbContext.Blogs.FindAsync(id);
            if (blog == null)
            {
                return NotFound();
            }

            _dbContext.Blogs.Remove(blog);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Xóa bài viết thành công.";
            return RedirectToAction(nameof(Index));
        }

        private int? GetCurrentUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idStr, out var id) ? id : null;
        }

        private static string GenerateSlug(string title)
        {
            var slug = title.ToLowerInvariant().Trim();
            slug = string.Join("-", slug.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return slug.Length > 200 ? slug.Substring(0, 200) : slug;
        }

        private async Task<string> SaveBlogImage(Microsoft.AspNetCore.Http.IFormFile file)
        {
            var uploadsPath = @"C:\img_hotel";
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            return filePath;
        }
    }
}
