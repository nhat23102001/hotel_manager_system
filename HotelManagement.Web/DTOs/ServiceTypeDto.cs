using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.DTOs
{
    public class ServiceTypeDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên loại dịch vụ là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Tên loại dịch vụ")]
        public string Name { get; set; } = string.Empty;
    }
}
