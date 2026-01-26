using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HotelManagement.Web.DTOs
{
    public class BlogDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Slug")]
        public string? Slug { get; set; }

        [StringLength(300)]
        [Display(Name = "Tóm tắt")]
        public string? Summary { get; set; }

        [Display(Name = "Nội dung")]
        public string? Content { get; set; }

        [StringLength(300)]
        [Display(Name = "Đường dẫn ảnh đại diện")]
        public string? Thumbnail { get; set; }

        [Display(Name = "Chọn ảnh đại diện")]
        public IFormFile? ThumbnailFile { get; set; }

        [Display(Name = "Công khai")]
        public bool IsPublished { get; set; } = true;

        [Display(Name = "Ngày xuất bản")]
        public DateTime? PublishedAt { get; set; }

        public string? AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? ThumbnailFileName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Thumbnail))
                    return null;
                
                var lastBackslash = Thumbnail.LastIndexOf('\\');
                var lastSlash = Thumbnail.LastIndexOf('/');
                var index = Math.Max(lastBackslash, lastSlash);
                
                return index >= 0 ? Thumbnail.Substring(index + 1) : Thumbnail;
            }
        }
    }
}
