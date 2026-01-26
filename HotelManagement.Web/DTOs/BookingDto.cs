using System;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.DTOs
{
    public class BookingDto
    {
        public int Id { get; set; }

        [Display(Name = "Mã đặt phòng")]
        public string BookingCode { get; set; } = string.Empty;

        [Display(Name = "Tên khách")]
        public string GuestName { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Ngày đặt")]
        public DateTime BookingDate { get; set; }

        [Display(Name = "Ngày nhận phòng")]
        public DateTime CheckInDate { get; set; }

        [Display(Name = "Ngày trả phòng")]
        public DateTime CheckOutDate { get; set; }

        [Display(Name = "Số đêm")]
        public int TotalNights { get; set; }

        [Display(Name = "Tạm tính")]
        public decimal SubTotal { get; set; }

        [Display(Name = "VAT")]
        public decimal VAT { get; set; }

        [Display(Name = "Tổng tiền")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Trạng thái")]
        public string? Status { get; set; }

        public string? Username { get; set; }
    }
}
