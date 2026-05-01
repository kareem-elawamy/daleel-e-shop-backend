using System;
using System.Collections.Generic;

namespace daleel_e_shop.BLL.DTOs.Orders
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ShippingAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}
