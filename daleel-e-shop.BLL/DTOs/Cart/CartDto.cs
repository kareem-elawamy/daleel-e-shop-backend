using System.Collections.Generic;

namespace daleel_e_shop.BLL.DTOs.Cart
{
    public class CartDto
    {
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }
    }
}
