using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HotelManagement.Web.DTOs
{
    public class RoomDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã phòng")]
        [Display(Name = "Mã phòng")]
        [MaxLength(50)]
        public string RoomCode { get; set; } = string.Empty;

        [Display(Name = "Tên phòng")]
        [MaxLength(150)]
        public string? RoomName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại phòng")]
        [Display(Name = "Loại phòng")]
        public int RoomTypeId { get; set; }

        public string? RoomType { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá phòng")]
        [Display(Name = "Giá theo đêm (VNĐ)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal PricePerNight { get; set; }

        [Display(Name = "Số người tối đa")]
        [Range(1, 20, ErrorMessage = "Số người từ 1-20")]
        public int? MaxPeople { get; set; }

        [Display(Name = "Trạng thái")]
        [MaxLength(20)]
        public string? Status { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Hình ảnh URL")]
        [MaxLength(255)]
        public string? ImageUrl { get; set; }

        [Display(Name = "Chọn ảnh phòng")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        // Helper property similar to BlogDto.ThumbnailFileName
        public string? ImageFileName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ImageUrl))
                    return null;
                
                var lastBackslash = ImageUrl.LastIndexOf('\\');
                var lastSlash = ImageUrl.LastIndexOf('/');
                var index = Math.Max(lastBackslash, lastSlash);
                
                return index >= 0 ? ImageUrl.Substring(index + 1) : ImageUrl;
            }
        }
    }
}

