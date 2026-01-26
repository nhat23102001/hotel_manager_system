using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.DTOs
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }
}
