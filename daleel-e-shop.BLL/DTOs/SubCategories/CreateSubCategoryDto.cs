using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace daleel_e_shop.BLL.DTOs.SubCategories
{
    public class CreateSubCategoryDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public IFormFile? Image { get; set; }

        [Required]
        public int CategoryId { get; set; }
    }
}
