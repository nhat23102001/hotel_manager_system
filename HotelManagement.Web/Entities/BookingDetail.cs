namespace HotelManagement.Web.Entities
{
    public class BookingDetail
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int RoomId { get; set; }
        public decimal PricePerNight { get; set; }

        public Booking? Booking { get; set; }
        public Room? Room { get; set; }
    }
}
