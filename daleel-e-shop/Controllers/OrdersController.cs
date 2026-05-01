using daleel_e_shop.BLL.DTOs.Orders;
using daleel_e_shop.BLL.Services.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace daleel_e_shop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var order = await _orderService.CreateOrderAsync(userId, dto);
            if (order == null)
                return BadRequest("Failed to create order. Please check product availability and stock.");

            return CreatedAtAction(nameof(GetMyOrderById), new { id = order.Id }, order);
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(orders);
        }

        [HttpGet("my/{id}")]
        [Authorize]
        public async Task<IActionResult> GetMyOrderById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var order = await _orderService.GetOrderByIdAsync(id, userId);
            if (order == null) return NotFound($"Order with ID {id} not found.");

            return Ok(order);
        }

        [HttpPut("my/{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _orderService.CancelOrderAsync(id, userId);
            if (!result)
                return BadRequest("Cannot cancel this order. It may not exist or is no longer in Pending status.");

            return Ok(new { Message = "Order cancelled successfully." });
        }

        // Admin endpoints

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var order = await _orderService.UpdateOrderStatusAsync(id, dto);
            if (order == null) return NotFound($"Order with ID {id} not found.");

            return Ok(order);
        }
    }
}
