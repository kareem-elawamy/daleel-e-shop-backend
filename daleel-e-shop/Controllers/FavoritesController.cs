using daleel_e_shop.BLL.Services.Favorites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace daleel_e_shop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;

        public FavoritesController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        [HttpPost("{productId}")]
        public async Task<IActionResult> ToggleFavorite(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var isAdded = await _favoriteService.ToggleFavoriteAsync(userId, productId);

            return Ok(new
            {
                IsFavorite = isAdded,
                Message = isAdded ? "Product added to favorites." : "Product removed from favorites."
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyFavorites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var favorites = await _favoriteService.GetUserFavoritesAsync(userId);
            return Ok(favorites);
        }

        [HttpGet("{productId}/check")]
        public async Task<IActionResult> IsFavorite(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var isFavorite = await _favoriteService.IsFavoriteAsync(userId, productId);
            return Ok(new { IsFavorite = isFavorite });
        }
    }
}
