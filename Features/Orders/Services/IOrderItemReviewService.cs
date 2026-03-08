using LeveLEO.Features.Orders.DTO;
using LeveLEO.Features.Products.DTO;

namespace LeveLEO.Features.Orders.Services;

public interface IOrderItemReviewService
{
    // Створення і оновлення
    Task<ReviewResponseDto> CreateReviewAsync(string userId, CreateReviewDto dto);

    Task<ReviewResponseDto> UpdateReviewAsync(string userId, Guid reviewId, UpdateReviewDto dto);

    Task DeleteReviewAsync(string userId, Guid reviewId);

    // Отримання
    Task<ReviewResponseDto> GetReviewByIdAsync(Guid reviewId);

    Task<List<ReviewResponseDto>> GetUserReviewsAsync(string userId);

    Task<ReviewResponseDto?> GetReviewByOrderItemAsync(Guid orderItemId);

    // Для продуктів
    Task<ProductReviewsDto> GetProductReviewsAsync(Guid productId, int page = 1, int pageSize = 10, bool approvedOnly = true);

    // Адмін
    Task<ReviewResponseDto> ApproveReviewAsync(Guid reviewId);

    Task<ReviewResponseDto> RejectReviewAsync(Guid reviewId);

    Task<PagedResultDto<ReviewResponseDto>> GetPendingReviewsAsync(int page = 1, int pageSize = 20);
}