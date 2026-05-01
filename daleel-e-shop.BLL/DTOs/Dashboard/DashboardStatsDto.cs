using System;
using System.Collections.Generic;

namespace daleel_e_shop.BLL.DTOs.Dashboard
{
    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int TotalCategories { get; set; }
        public int LowStockProducts { get; set; }
        public List<TopProductDto> TopSellingProducts { get; set; } = new List<TopProductDto>();
        public List<RecentOrderDto> RecentOrders { get; set; } = new List<RecentOrderDto>();
        public List<MonthlySalesDto> MonthlySales { get; set; } = new List<MonthlySalesDto>();
    }

    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RecentOrderDto
    {
        public int OrderId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
    }

    public class MonthlySalesDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalSales { get; set; }
        public int OrderCount { get; set; }
    }
}
