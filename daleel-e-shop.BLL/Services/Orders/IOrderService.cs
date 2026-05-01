using daleel_e_shop.BLL.DTOs.Orders;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Orders
{
    public interface IOrderService
    {
        Task<OrderDto?> CreateOrderAsync(string userId, CreateOrderDto dto);
        Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId);
        Task<OrderDto?> GetOrderByIdAsync(int id, string userId);
        Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
        Task<OrderDto?> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto dto);
        Task<bool> CancelOrderAsync(int id, string userId);
    }
}
