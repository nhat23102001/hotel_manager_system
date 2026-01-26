using System.Collections.Generic;

namespace HotelManagement.Web.Entities
{
    public class Service
    {
        public int Id { get; set; }
        public int ServiceTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Unit { get; set; }
        public bool IsActive { get; set; } = true;

        public ServiceType? ServiceType { get; set; }
        public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
    }
}
