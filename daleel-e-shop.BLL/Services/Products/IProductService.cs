using daleel_e_shop.BLL.DTOs.Products;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Products
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllAsync();
        Task<ProductDto?> GetByIdAsync(int id);
        Task<IEnumerable<ProductDto>> GetBySubCategoryAsync(int subCategoryId);
        Task<PagedResult<ProductDto>> SearchAsync(ProductSearchQuery query);
        Task<ProductDto> CreateAsync(CreateProductDto dto);
        Task<ProductDto?> UpdateAsync(int id, CreateProductDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<ProductImageDto>> GetProductImagesAsync(int productId);
        Task<ProductImageDto?> AddProductImageAsync(int productId, IFormFile image, bool isPrimary = false);
        Task<bool> DeleteProductImageAsync(int imageId);
        Task<bool> SetPrimaryImageAsync(int productId, int imageId);
    }
}
