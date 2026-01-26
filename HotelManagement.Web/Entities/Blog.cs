using System;

namespace HotelManagement.Web.Entities
{
    public class Blog
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Summary { get; set; }
        public string? Content { get; set; }
        public string? Thumbnail { get; set; }
        public int? AuthorId { get; set; }
        public bool IsPublished { get; set; } = true;
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? Author { get; set; }
    }
}
