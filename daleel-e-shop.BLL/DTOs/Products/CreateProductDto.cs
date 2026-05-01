using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace daleel_e_shop.BLL.DTOs.Products
{
    public class CreateProductDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Required]
        public int SubCategoryId { get; set; }

        public IFormFile? Image { get; set; }
    }
}
