using daleel_e_shop.BLL.DTOs.Dashboard;
using daleel_e_shop.DAL.Models;
using daleel_e_shop.DAL.Repositories;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var allOrders = await _unitOfWork.Orders.FindAsync(o => true, new[] { "User", "OrderItems", "OrderItems.Product" });
            var orderList = allOrders.ToList();

            var allProducts = await _unitOfWork.Products.GetAllAsync();
            var productList = allProducts.ToList();

            var allCategories = await _unitOfWork.Categories.GetAllAsync();

            var stats = new DashboardStatsDto
            {
                TotalUsers = _userManager.Users.Count(),
                TotalProducts = productList.Count,
                TotalOrders = orderList.Count,
                TotalRevenue = orderList.Where(o => o.Status != "Cancelled").Sum(o => o.TotalAmount),
                PendingOrders = orderList.Count(o => o.Status == "Pending"),
                DeliveredOrders = orderList.Count(o => o.Status == "Delivered"),
                CancelledOrders = orderList.Count(o => o.Status == "Cancelled"),
                TotalCategories = allCategories.Count(),
                LowStockProducts = productList.Count(p => p.StockQuantity <= 5 && p.IsActive),
            };

            // Top 5 selling products
            var completedOrderItems = orderList
                .Where(o => o.Status != "Cancelled")
                .SelectMany(o => o.OrderItems)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product?.Name ?? "Unknown",
                    ImageUrl = g.First().Product?.ImageUrl,
                    TotalSold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToList();

            stats.TopSellingProducts = completedOrderItems;

            // Recent 10 orders
            stats.RecentOrders = orderList
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new RecentOrderDto
                {
                    OrderId = o.Id,
                    UserEmail = o.User?.Email ?? string.Empty,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    OrderDate = o.OrderDate
                })
                .ToList();

            // Monthly sales for the last 6 months
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            stats.MonthlySales = orderList
                .Where(o => o.Status != "Cancelled" && o.OrderDate >= sixMonthsAgo)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new MonthlySalesDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalSales = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            return stats;
        }
    }
}
