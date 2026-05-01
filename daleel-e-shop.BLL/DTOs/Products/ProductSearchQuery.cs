using System.Collections.Generic;

namespace daleel_e_shop.BLL.DTOs.Products
{
    public class ProductSearchQuery
    {
        public string? Search { get; set; }
        public int? SubCategoryId { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SortBy { get; set; } // name, price, newest, rating
        public bool SortDescending { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
