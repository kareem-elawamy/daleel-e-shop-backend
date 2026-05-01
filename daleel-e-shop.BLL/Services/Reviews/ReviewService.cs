using daleel_e_shop.BLL.DTOs.Reviews;
using daleel_e_shop.DAL.Models;
using daleel_e_shop.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Reviews
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReviewService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ReviewDto?> CreateReviewAsync(string userId, CreateReviewDto dto)
        {
            // Check product exists
            var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
            if (product == null) return null;

            // Check if user already reviewed this product
            var existing = await _unitOfWork.Reviews.FindSingleAsync(
                r => r.UserId == userId && r.ProductId == dto.ProductId);
            if (existing != null) return null;

            var review = new Review
            {
                UserId = userId,
                ProductId = dto.ProductId,
                Rating = dto.Rating,
                Comment = dto.Comment
            };

            await _unitOfWork.Reviews.AddAsync(review);
            await _unitOfWork.CompleteAsync();

            var created = await _unitOfWork.Reviews.FindSingleAsync(
                r => r.Id == review.Id, new[] { "User" });
            return MapToDto(created!);
        }

        public async Task<ReviewDto?> UpdateReviewAsync(string userId, int reviewId, CreateReviewDto dto)
        {
            var review = await _unitOfWork.Reviews.FindSingleAsync(
                r => r.Id == reviewId && r.UserId == userId, new[] { "User" });
            if (review == null) return null;

            review.Rating = dto.Rating;
            review.Comment = dto.Comment;

            _unitOfWork.Reviews.Update(review);
            await _unitOfWork.CompleteAsync();

            return MapToDto(review);
        }

        public async Task<bool> DeleteReviewAsync(string userId, int reviewId)
        {
            var review = await _unitOfWork.Reviews.FindSingleAsync(
                r => r.Id == reviewId && r.UserId == userId);
            if (review == null) return false;

            _unitOfWork.Reviews.Delete(review);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<IEnumerable<ReviewDto>> GetProductReviewsAsync(int productId)
        {
            var reviews = await _unitOfWork.Reviews.FindAsync(
                r => r.ProductId == productId, new[] { "User" });

            return reviews.OrderByDescending(r => r.CreatedAt).Select(MapToDto);
        }

        public async Task<ProductRatingSummaryDto> GetProductRatingAsync(int productId)
        {
            var reviews = await _unitOfWork.Reviews.FindAsync(r => r.ProductId == productId);
            var reviewList = reviews.ToList();

            return new ProductRatingSummaryDto
            {
                ProductId = productId,
                AverageRating = reviewList.Any() ? reviewList.Average(r => r.Rating) : 0,
                TotalReviews = reviewList.Count,
                FiveStarCount = reviewList.Count(r => r.Rating == 5),
                FourStarCount = reviewList.Count(r => r.Rating == 4),
                ThreeStarCount = reviewList.Count(r => r.Rating == 3),
                TwoStarCount = reviewList.Count(r => r.Rating == 2),
                OneStarCount = reviewList.Count(r => r.Rating == 1)
            };
        }

        private ReviewDto MapToDto(Review review)
        {
            return new ReviewDto
            {
                Id = review.Id,
                ProductId = review.ProductId,
                UserId = review.UserId,
                UserName = review.User?.UserName ?? string.Empty,
                UserImageUrl = review.User?.ImageUrl,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            };
        }
    }
}
