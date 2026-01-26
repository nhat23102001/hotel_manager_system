using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

            [Display(Name = "Ghi nhớ đăng nhập")]
            public bool RememberMe { get; set; }
    }
}
