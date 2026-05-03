using daleel_e_shop.BLL.DTOs.Products;
using daleel_e_shop.BLL.Services.Files;
using daleel_e_shop.DAL.Models;
using daleel_e_shop.DAL.Repositories;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;

        public ProductService(IUnitOfWork unitOfWork, IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        public async Task<PagedResult<ProductDto>> GetAllAsync(int page = 1, int pageSize = 20)
        {
            var all = await _unitOfWork.Products.FindAsync(p => true, new[] { "SubCategory", "Images", "Reviews" });
            var totalCount = all.Count();
            var items = all.Skip((page - 1) * pageSize).Take(pageSize).Select(MapToDto).ToList();
            return new PagedResult<ProductDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var product = await _unitOfWork.Products.FindSingleAsync(p => p.Id == id, new[] { "SubCategory", "Images", "Reviews" });
            if (product == null) return null;
            return MapToDto(product);
        }

        public async Task<PagedResult<ProductDto>> GetBySubCategoryAsync(int subCategoryId, int page = 1, int pageSize = 20)
        {
            var all = await _unitOfWork.Products.FindAsync(
                p => p.SubCategoryId == subCategoryId && p.IsActive,
                new[] { "SubCategory", "Images", "Reviews" });
            var totalCount = all.Count();
            var items = all.Skip((page - 1) * pageSize).Take(pageSize).Select(MapToDto).ToList();
            return new PagedResult<ProductDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<PagedResult<ProductDto>> SearchAsync(ProductSearchQuery query)
        {
            var products = await _unitOfWork.Products.FindAsync(
                p => p.IsActive, new[] { "SubCategory", "Images", "Reviews" });

            var filtered = products.AsQueryable();

            // Search by name or description
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var searchLower = query.Search.ToLower();
                filtered = filtered.Where(p =>
                    p.Name.ToLower().Contains(searchLower) ||
                    p.Description.ToLower().Contains(searchLower));
            }

            // Filter by SubCategory
            if (query.SubCategoryId.HasValue)
                filtered = filtered.Where(p => p.SubCategoryId == query.SubCategoryId.Value);

            // Filter by Category
            if (query.CategoryId.HasValue)
                filtered = filtered.Where(p => p.SubCategory != null && p.SubCategory.CategoryId == query.CategoryId.Value);

            // Filter by price range
            if (query.MinPrice.HasValue)
                filtered = filtered.Where(p => (p.DiscountPrice ?? p.Price) >= query.MinPrice.Value);
            if (query.MaxPrice.HasValue)
                filtered = filtered.Where(p => (p.DiscountPrice ?? p.Price) <= query.MaxPrice.Value);

            // Sorting
            filtered = query.SortBy?.ToLower() switch
            {
                "name" => query.SortDescending ? filtered.OrderByDescending(p => p.Name) : filtered.OrderBy(p => p.Name),
                "price" => query.SortDescending
                    ? filtered.OrderByDescending(p => p.DiscountPrice ?? p.Price)
                    : filtered.OrderBy(p => p.DiscountPrice ?? p.Price),
                "newest" => filtered.OrderByDescending(p => p.CreatedAt),
                "rating" => filtered.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0),
                _ => filtered.OrderByDescending(p => p.CreatedAt)
            };

            var totalCount = filtered.Count();

            // Pagination
            var pageSize = query.PageSize > 0 ? query.PageSize : 10;
            var page = query.Page > 0 ? query.Page : 1;

            var pagedItems = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResult<ProductDto>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                DiscountPrice = dto.DiscountPrice,
                StockQuantity = dto.StockQuantity,
                SubCategoryId = dto.SubCategoryId
            };

            if (dto.Image != null)
            {
                product.ImageUrl = await _fileService.SaveFileAsync(dto.Image, "products");
            }

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.CompleteAsync();

            // Reload with SubCategory
            var created = await _unitOfWork.Products.FindSingleAsync(p => p.Id == product.Id, new[] { "SubCategory", "Images", "Reviews" });
            return MapToDto(created!);
        }

        public async Task<ProductDto?> UpdateAsync(int id, CreateProductDto dto)
        {
            var product = await _unitOfWork.Products.FindSingleAsync(p => p.Id == id, new[] { "SubCategory", "Images", "Reviews" });
            if (product == null) return null;

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.DiscountPrice = dto.DiscountPrice;
            product.StockQuantity = dto.StockQuantity;
            product.SubCategoryId = dto.SubCategoryId;

            if (dto.Image != null)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    _fileService.DeleteFile(product.ImageUrl);
                }
                product.ImageUrl = await _fileService.SaveFileAsync(dto.Image, "products");
            }

            _unitOfWork.Products.Update(product);
            await _unitOfWork.CompleteAsync();

            return MapToDto(product);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _unitOfWork.Products.FindSingleAsync(p => p.Id == id, new[] { "Images" });
            if (product == null) return false;

            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                _fileService.DeleteFile(product.ImageUrl);
            }

            // Delete all product images
            if (product.Images != null)
            {
                foreach (var img in product.Images)
                {
                    _fileService.DeleteFile(img.ImageUrl);
                    _unitOfWork.ProductImages.Delete(img);
                }
            }

            _unitOfWork.Products.Delete(product);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        // Product Images
        public async Task<IEnumerable<ProductImageDto>> GetProductImagesAsync(int productId)
        {
            var images = await _unitOfWork.ProductImages.FindAsync(i => i.ProductId == productId);
            return images.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageDto
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                IsPrimary = i.IsPrimary,
                DisplayOrder = i.DisplayOrder
            });
        }

        public async Task<ProductImageDto?> AddProductImageAsync(int productId, IFormFile image, bool isPrimary = false)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null) return null;

            var imageUrl = await _fileService.SaveFileAsync(image, "products");

            // If setting as primary, unset existing primary
            if (isPrimary)
            {
                var existingImages = await _unitOfWork.ProductImages.FindAsync(i => i.ProductId == productId && i.IsPrimary);
                foreach (var img in existingImages)
                {
                    img.IsPrimary = false;
                    _unitOfWork.ProductImages.Update(img);
                }
            }

            var existingCount = (await _unitOfWork.ProductImages.FindAsync(i => i.ProductId == productId)).Count();

            var productImage = new ProductImage
            {
                ProductId = productId,
                ImageUrl = imageUrl,
                IsPrimary = isPrimary,
                DisplayOrder = existingCount
            };

            await _unitOfWork.ProductImages.AddAsync(productImage);
            await _unitOfWork.CompleteAsync();

            return new ProductImageDto
            {
                Id = productImage.Id,
                ImageUrl = productImage.ImageUrl,
                IsPrimary = productImage.IsPrimary,
                DisplayOrder = productImage.DisplayOrder
            };
        }

        public async Task<bool> DeleteProductImageAsync(int imageId)
        {
            var image = await _unitOfWork.ProductImages.GetByIdAsync(imageId);
            if (image == null) return false;

            _fileService.DeleteFile(image.ImageUrl);
            _unitOfWork.ProductImages.Delete(image);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> SetPrimaryImageAsync(int productId, int imageId)
        {
            var image = await _unitOfWork.ProductImages.FindSingleAsync(
                i => i.Id == imageId && i.ProductId == productId);
            if (image == null) return false;

            // Unset all primary
            var allImages = await _unitOfWork.ProductImages.FindAsync(i => i.ProductId == productId);
            foreach (var img in allImages)
            {
                img.IsPrimary = img.Id == imageId;
                _unitOfWork.ProductImages.Update(img);
            }

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<PagedResult<ProductDto>> GetDealsAsync(int page = 1, int pageSize = 20)
        {
            var all = await _unitOfWork.Products.FindAsync(
                p => p.IsActive && p.DiscountPrice != null && p.DiscountPrice < p.Price,
                new[] { "SubCategory", "Images", "Reviews" });
            var sorted = all.OrderByDescending(p => p.CreatedAt).ToList();
            var totalCount = sorted.Count;
            var items = sorted.Skip((page - 1) * pageSize).Take(pageSize).Select(MapToDto).ToList();
            return new PagedResult<ProductDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<PagedResult<ProductDto>> GetBestSellersAsync(int page = 1, int pageSize = 20)
        {
            var orderItems = await _unitOfWork.OrderItems.GetAllAsync();
            var bestSellerIds = orderItems
                .GroupBy(oi => oi.ProductId)
                .OrderByDescending(g => g.Sum(oi => oi.Quantity))
                .Select(g => g.Key)
                .ToList();

            List<ProductDto> orderedDtos;

            if (!bestSellerIds.Any())
            {
                // Fallback: return top rated products if no orders exist
                var fallback = await _unitOfWork.Products.FindAsync(p => p.IsActive, new[] { "SubCategory", "Images", "Reviews" });
                orderedDtos = fallback
                    .OrderByDescending(p => p.Reviews != null && p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0)
                    .Select(MapToDto)
                    .ToList();
            }
            else
            {
                var products = await _unitOfWork.Products.FindAsync(
                    p => bestSellerIds.Contains(p.Id) && p.IsActive,
                    new[] { "SubCategory", "Images", "Reviews" });
                orderedDtos = bestSellerIds
                    .Select(id => products.FirstOrDefault(p => p.Id == id))
                    .Where(p => p != null)
                    .Select(MapToDto!)
                    .ToList();
            }

            var totalCount = orderedDtos.Count;
            var items = orderedDtos.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new PagedResult<ProductDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        private ProductDto MapToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                SubCategoryId = product.SubCategoryId,
                SubCategoryName = product.SubCategory?.Name ?? string.Empty,
                AverageRating = product.Reviews != null && product.Reviews.Any()
                    ? product.Reviews.Average(r => r.Rating)
                    : 0,
                TotalReviews = product.Reviews?.Count ?? 0,
                Images = product.Images?.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsPrimary = i.IsPrimary,
                    DisplayOrder = i.DisplayOrder
                }).ToList() ?? new List<ProductImageDto>()
            };
        }
    }
}
