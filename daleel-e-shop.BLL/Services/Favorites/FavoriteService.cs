using daleel_e_shop.BLL.DTOs.Favorites;
using daleel_e_shop.DAL.Models;
using daleel_e_shop.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Favorites
{
    public class FavoriteService : IFavoriteService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FavoriteService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> ToggleFavoriteAsync(string userId, int productId)
        {
            // Check if product exists
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null) return false;

            var existing = await _unitOfWork.Favorites.FindSingleAsync(
                f => f.UserId == userId && f.ProductId == productId);

            if (existing != null)
            {
                // Remove from favorites
                _unitOfWork.Favorites.Delete(existing);
                await _unitOfWork.CompleteAsync();
                return false; // Removed
            }
            else
            {
                // Add to favorites
                var favorite = new Favorite
                {
                    UserId = userId,
                    ProductId = productId
                };
                await _unitOfWork.Favorites.AddAsync(favorite);
                await _unitOfWork.CompleteAsync();
                return true; // Added
            }
        }

        public async Task<IEnumerable<FavoriteDto>> GetUserFavoritesAsync(string userId)
        {
            var favorites = await _unitOfWork.Favorites.FindAsync(
                f => f.UserId == userId,
                new[] { "Product" });

            return favorites.Select(f => new FavoriteDto
            {
                Id = f.Id,
                ProductId = f.ProductId,
                ProductName = f.Product?.Name ?? string.Empty,
                ProductImageUrl = f.Product?.ImageUrl,
                ProductPrice = f.Product?.Price ?? 0,
                ProductDiscountPrice = f.Product?.DiscountPrice,
                CreatedAt = f.CreatedAt
            });
        }

        public async Task<bool> IsFavoriteAsync(string userId, int productId)
        {
            var favorite = await _unitOfWork.Favorites.FindSingleAsync(
                f => f.UserId == userId && f.ProductId == productId);
            return favorite != null;
        }
    }
}
