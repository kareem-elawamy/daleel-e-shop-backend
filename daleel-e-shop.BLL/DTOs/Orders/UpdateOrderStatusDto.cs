using System.ComponentModel.DataAnnotations;

namespace daleel_e_shop.BLL.DTOs.Orders
{
    public class UpdateOrderStatusDto
    {
        [Required]
        [RegularExpression("^(Pending|Confirmed|Shipped|Delivered|Cancelled)$",
            ErrorMessage = "Status must be one of: Pending, Confirmed, Shipped, Delivered, Cancelled.")]
        public string Status { get; set; } = string.Empty;
    }
}
