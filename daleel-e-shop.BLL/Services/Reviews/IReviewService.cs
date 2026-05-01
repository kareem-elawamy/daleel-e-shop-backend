using daleel_e_shop.BLL.DTOs.Reviews;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Reviews
{
    public interface IReviewService
    {
        Task<ReviewDto?> CreateReviewAsync(string userId, CreateReviewDto dto);
        Task<ReviewDto?> UpdateReviewAsync(string userId, int reviewId, CreateReviewDto dto);
        Task<bool> DeleteReviewAsync(string userId, int reviewId);
        Task<IEnumerable<ReviewDto>> GetProductReviewsAsync(int productId);
        Task<ProductRatingSummaryDto> GetProductRatingAsync(int productId);
    }
}
