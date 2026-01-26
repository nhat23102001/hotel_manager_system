using System.Collections.Generic;

namespace HotelManagement.Web.Entities
{
    public class Room
    {
        public int Id { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public string? RoomName { get; set; }
        public int RoomTypeId { get; set; }
        public decimal PricePerNight { get; set; }
        public int? MaxPeople { get; set; }
        public string? Status { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;

        public RoomType? RoomType { get; set; }
        public ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
    }
}
