using System;
using System.Collections.Generic;

namespace HotelManagement.Web.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateTime BookingDate { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int TotalNights { get; set; }
        public decimal SubTotal { get; set; }
        public decimal VAT { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
        public ICollection<BookingDetail> Details { get; set; } = new List<BookingDetail>();
        public ICollection<BookingService> Services { get; set; } = new List<BookingService>();
    }
}
