using System.Collections.Generic;

namespace HotelManagement.Web.Entities
{
    public class RoomType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? BasePrice { get; set; }
        public int? MaxPeople { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
