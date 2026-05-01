using System;

namespace daleel_e_shop.BLL.DTOs.Favorites
{
    public class FavoriteDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public decimal ProductPrice { get; set; }
        public decimal? ProductDiscountPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
