using HotelManagement.Web.Data;
using HotelManagement.Web.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Areas.Client.Controllers
{
    [Area("Client")]
    public class BlogController : Controller
    {
        private readonly HotelManagementDbContext _dbContext;

        public BlogController(HotelManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(int page = 1, string? search = null)
        {
            int pageSize = 6;
            var query = _dbContext.Blogs
                .Where(b => b.IsPublished)
                .Include(b => b.Author)
                    .ThenInclude(a => a.Profile)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b => b.Title.Contains(search) ||
                                       (b.Summary != null && b.Summary.Contains(search)));
            }

            var totalBlogs = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalBlogs / pageSize);

            var blogs = await query
                .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BlogDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Slug = b.Slug,
                    Summary = b.Summary,
                    Content = b.Content,
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

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;

            return View("Index", blogs);
        }

        public async Task<IActionResult> Detail(string slug)
        {
            var blog = await _dbContext.Blogs
                .Where(b => b.IsPublished && b.Slug == slug)
                .Include(b => b.Author)
                    .ThenInclude(a => a.Profile)
                .FirstOrDefaultAsync();

            if (blog == null)
                return NotFound();

            var blogDto = new BlogDto
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

            var relatedBlogs = await _dbContext.Blogs
                .Where(b => b.IsPublished && b.Id != blog.Id)
                .Include(b => b.Author)
                    .ThenInclude(a => a.Profile)
                .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
                .Take(4)
                .Select(b => new BlogDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Slug = b.Slug,
                    Summary = b.Summary,
                    Thumbnail = b.Thumbnail,
                    PublishedAt = b.PublishedAt,
                    CreatedAt = b.CreatedAt,
                    AuthorName = b.Author != null
                        ? (b.Author.Profile != null && !string.IsNullOrWhiteSpace(b.Author.Profile.FullName)
                            ? b.Author.Profile.FullName
                            : b.Author.Username)
                        : null
                })
                .ToListAsync();

            ViewBag.RelatedBlogs = relatedBlogs;
            return View("Detail", blogDto);
        }
    }
}
