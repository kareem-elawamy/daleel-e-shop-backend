using System.ComponentModel.DataAnnotations;

namespace daleel_e_shop.BLL.DTOs.Cart
{
    public class CheckoutDto
    {
        [MaxLength(500)]
        public string? ShippingAddress { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
    }
}
