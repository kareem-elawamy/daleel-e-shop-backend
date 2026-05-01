using daleel_e_shop.BLL.DTOs.Cart;
using daleel_e_shop.BLL.DTOs.Orders;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Cart
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(string userId);
        Task<CartItemDto?> AddToCartAsync(string userId, AddToCartDto dto);
        Task<CartItemDto?> UpdateCartItemAsync(string userId, int cartItemId, UpdateCartItemDto dto);
        Task<bool> RemoveFromCartAsync(string userId, int cartItemId);
        Task<bool> ClearCartAsync(string userId);
        Task<OrderDto?> CheckoutAsync(string userId, string? shippingAddress, string? phoneNumber);
    }
}
