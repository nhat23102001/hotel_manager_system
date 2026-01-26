using System;

namespace HotelManagement.Web.Entities
{
    public class Contact
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? ReplyContent { get; set; }
        public int? RepliedBy { get; set; }
        public DateTime? RepliedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? RepliedByUser { get; set; }
    }
}
