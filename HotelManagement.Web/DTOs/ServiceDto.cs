using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.DTOs
{
    public class ServiceDto
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Loại dịch vụ")]
        public int ServiceTypeId { get; set; }

        [Required(ErrorMessage = "Tên dịch vụ là bắt buộc")]
        [StringLength(150)]
        [Display(Name = "Tên dịch vụ")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Đơn giá")]
        public decimal UnitPrice { get; set; }

        [StringLength(50)]
        [Display(Name = "Đơn vị tính")]
        public string? Unit { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        public string? ServiceTypeName { get; set; }
    }
}
