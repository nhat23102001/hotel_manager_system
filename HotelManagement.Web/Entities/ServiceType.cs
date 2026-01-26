using System.Collections.Generic;

namespace HotelManagement.Web.Entities
{
    public class ServiceType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<Service> Services { get; set; } = new List<Service>();
    }
}
