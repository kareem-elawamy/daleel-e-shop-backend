using System.ComponentModel.DataAnnotations;

namespace daleel_e_shop.BLL.DTOs.Reviews
{
    public class CreateReviewDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
