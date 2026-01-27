using System;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.DTOs
{
    public class CreateBookingDto
    {
        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng")]
        [Display(Name = "Ngày nhận phòng")]
        public DateTime CheckInDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng")]
        [Display(Name = "Ngày trả phòng")]
        public DateTime CheckOutDate { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khách hàng")]
        [Display(Name = "Tên khách hàng")]
        [StringLength(100)]
        public string GuestName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Địa chỉ")]
        [StringLength(200)]
        public string? Address { get; set; }

        [Display(Name = "Ghi chú")]
        [StringLength(500)]
        public string? Notes { get; set; }

        public int RoomId { get; set; }
        
        // Thêm UserId để nhận từ form
        public int? UserId { get; set; }

        public List<int> ServiceIds { get; set; } = new List<int>();
    }
}
