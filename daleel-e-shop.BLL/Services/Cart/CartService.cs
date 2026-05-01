using daleel_e_shop.BLL.DTOs.Cart;
using daleel_e_shop.BLL.DTOs.Orders;
using daleel_e_shop.DAL.Models;
using daleel_e_shop.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Cart
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CartDto> GetCartAsync(string userId)
        {
            var cartItems = await _unitOfWork.CartItems.FindAsync(
                c => c.UserId == userId,
                new[] { "Product" });

            var items = cartItems.Select(MapToDto).ToList();

            return new CartDto
            {
                Items = items,
                TotalAmount = items.Sum(i => i.Subtotal),
                TotalItems = items.Sum(i => i.Quantity)
            };
        }

        public async Task<CartItemDto?> AddToCartAsync(string userId, AddToCartDto dto)
        {
            // Check product exists and is active
            var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
            if (product == null || !product.IsActive) return null;

            // Check if already in cart
            var existing = await _unitOfWork.CartItems.FindSingleAsync(
                c => c.UserId == userId && c.ProductId == dto.ProductId);

            if (existing != null)
            {
                // Update quantity
                existing.Quantity += dto.Quantity;
                _unitOfWork.CartItems.Update(existing);
                await _unitOfWork.CompleteAsync();

                // Reload with product
                var reloaded = await _unitOfWork.CartItems.FindSingleAsync(
                    c => c.Id == existing.Id, new[] { "Product" });
                return MapToDto(reloaded!);
            }

            var cartItem = new CartItem
            {
                UserId = userId,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity
            };

            await _unitOfWork.CartItems.AddAsync(cartItem);
            await _unitOfWork.CompleteAsync();

            // Reload with product
            var created = await _unitOfWork.CartItems.FindSingleAsync(
                c => c.Id == cartItem.Id, new[] { "Product" });
            return MapToDto(created!);
        }

        public async Task<CartItemDto?> UpdateCartItemAsync(string userId, int cartItemId, UpdateCartItemDto dto)
        {
            var cartItem = await _unitOfWork.CartItems.FindSingleAsync(
                c => c.Id == cartItemId && c.UserId == userId,
                new[] { "Product" });

            if (cartItem == null) return null;

            cartItem.Quantity = dto.Quantity;
            _unitOfWork.CartItems.Update(cartItem);
            await _unitOfWork.CompleteAsync();

            return MapToDto(cartItem);
        }

        public async Task<bool> RemoveFromCartAsync(string userId, int cartItemId)
        {
            var cartItem = await _unitOfWork.CartItems.FindSingleAsync(
                c => c.Id == cartItemId && c.UserId == userId);

            if (cartItem == null) return false;

            _unitOfWork.CartItems.Delete(cartItem);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> ClearCartAsync(string userId)
        {
            var cartItems = await _unitOfWork.CartItems.FindAsync(c => c.UserId == userId);
            foreach (var item in cartItems)
            {
                _unitOfWork.CartItems.Delete(item);
            }
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<OrderDto?> CheckoutAsync(string userId, string? shippingAddress, string? phoneNumber)
        {
            var cartItems = await _unitOfWork.CartItems.FindAsync(
                c => c.UserId == userId,
                new[] { "Product" });

            var cartList = cartItems.ToList();
            if (!cartList.Any()) return null;

            var order = new Order
            {
                UserId = userId,
                ShippingAddress = shippingAddress,
                PhoneNumber = phoneNumber,
                Status = "Pending"
            };

            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            foreach (var cartItem in cartList)
            {
                var product = cartItem.Product;
                if (product == null || !product.IsActive) return null;
                if (product.StockQuantity < cartItem.Quantity) return null;

                var unitPrice = product.DiscountPrice ?? product.Price;
                totalAmount += unitPrice * cartItem.Quantity;

                // Decrease stock
                product.StockQuantity -= cartItem.Quantity;
                _unitOfWork.Products.Update(product);

                orderItems.Add(new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = unitPrice
                });
            }

            order.TotalAmount = totalAmount;
            order.OrderItems = orderItems;

            await _unitOfWork.Orders.AddAsync(order);

            // Clear cart after checkout
            foreach (var cartItem in cartList)
            {
                _unitOfWork.CartItems.Delete(cartItem);
            }

            await _unitOfWork.CompleteAsync();

            // Reload order with includes
            var createdOrder = await _unitOfWork.Orders.FindSingleAsync(
                o => o.Id == order.Id,
                new[] { "User", "OrderItems", "OrderItems.Product" });

            if (createdOrder == null) return null;

            return new OrderDto
            {
                Id = createdOrder.Id,
                UserEmail = createdOrder.User?.Email ?? string.Empty,
                OrderDate = createdOrder.OrderDate,
                TotalAmount = createdOrder.TotalAmount,
                Status = createdOrder.Status,
                ShippingAddress = createdOrder.ShippingAddress,
                PhoneNumber = createdOrder.PhoneNumber,
                Items = createdOrder.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? string.Empty,
                    ProductImageUrl = oi.Product?.ImageUrl,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };
        }

        private CartItemDto MapToDto(CartItem cartItem)
        {
            var price = cartItem.Product?.DiscountPrice ?? cartItem.Product?.Price ?? 0;
            return new CartItemDto
            {
                Id = cartItem.Id,
                ProductId = cartItem.ProductId,
                ProductName = cartItem.Product?.Name ?? string.Empty,
                ProductImageUrl = cartItem.Product?.ImageUrl,
                ProductPrice = cartItem.Product?.Price ?? 0,
                ProductDiscountPrice = cartItem.Product?.DiscountPrice,
                Quantity = cartItem.Quantity,
                Subtotal = price * cartItem.Quantity
            };
        }
    }
}
