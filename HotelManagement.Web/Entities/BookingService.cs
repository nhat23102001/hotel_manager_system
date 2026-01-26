namespace HotelManagement.Web.Entities
{
    public class BookingService
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int ServiceId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        public Booking? Booking { get; set; }
        public Service? Service { get; set; }
    }
}
