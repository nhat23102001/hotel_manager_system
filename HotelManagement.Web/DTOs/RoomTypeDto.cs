using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.DTOs
{
    public class RoomTypeDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên loại phòng")]
        [Display(Name = "Tên loại phòng")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        [MaxLength(255)]
        public string? Description { get; set; }

        [Display(Name = "Giá cơ bản (VNĐ)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal? BasePrice { get; set; }

        [Display(Name = "Số người tối đa")]
        [Range(1, 20, ErrorMessage = "Số người từ 1-20")]
        public int? MaxPeople { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;
    }
}
