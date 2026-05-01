using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace daleel_e_shop.BLL.DTOs.Orders
{
    public class CreateOrderDto
    {
        [MaxLength(500)]
        public string? ShippingAddress { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
        public List<CreateOrderItemDto> Items { get; set; } = new List<CreateOrderItemDto>();
    }
}
