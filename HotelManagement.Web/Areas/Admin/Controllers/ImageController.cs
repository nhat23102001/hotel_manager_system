using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Image")]
    public class ImageController : Controller
    {
        [HttpGet("{filename}")]
        public IActionResult GetImage(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return NotFound();

            var uploadsPath = @"C:\img_hotel";
            var filePath = Path.Combine(uploadsPath, filename);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var contentType = GetContentType(filePath);

            return File(fileBytes, contentType);
        }

        private string GetContentType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}
