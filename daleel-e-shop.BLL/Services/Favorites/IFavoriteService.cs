using daleel_e_shop.BLL.DTOs.Favorites;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Favorites
{
    public interface IFavoriteService
    {
        Task<bool> ToggleFavoriteAsync(string userId, int productId);
        Task<IEnumerable<FavoriteDto>> GetUserFavoritesAsync(string userId);
        Task<bool> IsFavoriteAsync(string userId, int productId);
    }
}
