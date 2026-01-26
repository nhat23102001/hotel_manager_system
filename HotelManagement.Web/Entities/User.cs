using System;
using System.Collections.Generic;

namespace HotelManagement.Web.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Admin | Manager | Client
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public UserProfile? Profile { get; set; }
        public ICollection<Blog> Blogs { get; set; } = new List<Blog>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Contact> RepliedContacts { get; set; } = new List<Contact>();
    }
}
