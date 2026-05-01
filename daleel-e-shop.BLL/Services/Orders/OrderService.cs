using daleel_e_shop.BLL.DTOs.Orders;
using daleel_e_shop.BLL.Services.Notifications;
using daleel_e_shop.DAL.Models;
using daleel_e_shop.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Orders
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public OrderService(IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task<OrderDto?> CreateOrderAsync(string userId, CreateOrderDto dto)
        {
            var order = new Order
            {
                UserId = userId,
                ShippingAddress = dto.ShippingAddress,
                PhoneNumber = dto.PhoneNumber,
                Status = "Pending"
            };

            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in dto.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product == null || !product.IsActive)
                    return null; // Product not found or inactive

                if (product.StockQuantity < item.Quantity)
                    return null; // Not enough stock

                var unitPrice = product.DiscountPrice ?? product.Price;
                totalAmount += unitPrice * item.Quantity;

                // Decrease stock
                product.StockQuantity -= item.Quantity;
                _unitOfWork.Products.Update(product);

                orderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice
                });
            }

            order.TotalAmount = totalAmount;
            order.OrderItems = orderItems;

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.CompleteAsync();

            // Send notification
            await _notificationService.CreateNotificationAsync(
                userId,
                "Order Placed",
                $"Your order #{order.Id} has been placed successfully. Total: {totalAmount:C}",
                "Order",
                order.Id);

            // Reload with includes
            return await GetOrderDtoById(order.Id);
        }

        public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId)
        {
            var orders = await _unitOfWork.Orders.FindAsync(
                o => o.UserId == userId,
                new[] { "User", "OrderItems", "OrderItems.Product" });

            return orders.OrderByDescending(o => o.OrderDate).Select(MapToDto);
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int id, string userId)
        {
            var order = await _unitOfWork.Orders.FindSingleAsync(
                o => o.Id == id && o.UserId == userId,
                new[] { "User", "OrderItems", "OrderItems.Product" });

            if (order == null) return null;
            return MapToDto(order);
        }

        public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
        {
            var orders = await _unitOfWork.Orders.FindAsync(
                o => true,
                new[] { "User", "OrderItems", "OrderItems.Product" });

            return orders.OrderByDescending(o => o.OrderDate).Select(MapToDto);
        }

        public async Task<OrderDto?> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto dto)
        {
            var order = await _unitOfWork.Orders.FindSingleAsync(
                o => o.Id == id,
                new[] { "User", "OrderItems", "OrderItems.Product" });

            if (order == null) return null;

            order.Status = dto.Status;
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.CompleteAsync();

            // Send notification to user
            await _notificationService.CreateNotificationAsync(
                order.UserId,
                "Order Status Updated",
                $"Your order #{order.Id} status has been updated to: {dto.Status}",
                "Order",
                order.Id);

            return MapToDto(order);
        }

        public async Task<bool> CancelOrderAsync(int id, string userId)
        {
            var order = await _unitOfWork.Orders.FindSingleAsync(
                o => o.Id == id && o.UserId == userId,
                new[] { "OrderItems" });

            if (order == null) return false;
            if (order.Status != "Pending") return false; // Can only cancel pending orders

            order.Status = "Cancelled";

            // Restore stock
            foreach (var item in order.OrderItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                    _unitOfWork.Products.Update(product);
                }
            }

            _unitOfWork.Orders.Update(order);
            await _unitOfWork.CompleteAsync();

            // Send notification
            await _notificationService.CreateNotificationAsync(
                userId,
                "Order Cancelled",
                $"Your order #{order.Id} has been cancelled.",
                "Order",
                order.Id);

            return true;
        }

        private async Task<OrderDto?> GetOrderDtoById(int id)
        {
            var order = await _unitOfWork.Orders.FindSingleAsync(
                o => o.Id == id,
                new[] { "User", "OrderItems", "OrderItems.Product" });

            if (order == null) return null;
            return MapToDto(order);
        }

        private OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                UserEmail = order.User?.Email ?? string.Empty,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                ShippingAddress = order.ShippingAddress,
                PhoneNumber = order.PhoneNumber,
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? string.Empty,
                    ProductImageUrl = oi.Product?.ImageUrl,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };
        }
    }
}
