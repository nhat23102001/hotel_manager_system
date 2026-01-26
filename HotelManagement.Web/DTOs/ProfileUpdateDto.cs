using System;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.DTOs
{
    public class ProfileUpdateDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        [Display(Name = "Họ tên")]
        [MaxLength(150)]
        public string? FullName { get; set; }

        [Display(Name = "Email")]
        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }

        [Display(Name = "Số điện thoại")]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Địa chỉ")]
        [MaxLength(255)]
        public string? Address { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Giới tính")]
        [MaxLength(10)]
        public string? Gender { get; set; }

        [Display(Name = "Mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [Display(Name = "Mật khẩu mới")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")] 
        public string? NewPassword { get; set; }

        [Display(Name = "Xác nhận mật khẩu mới")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string? ConfirmPassword { get; set; }
    }
}
