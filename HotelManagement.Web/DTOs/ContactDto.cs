using System;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.DTOs
{
    public class ContactDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Tên người gửi")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        [Display(Name = "Nội dung")]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "Trạng thái")]
        public string? Status { get; set; }

        [Display(Name = "Phản hồi")]
        public string? ReplyContent { get; set; }

        public int? RepliedBy { get; set; }
        public string? RepliedByName { get; set; }
        public DateTime? RepliedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
