// Features/Orders/Services/OrderItemReviewService.cs
using LeveLEO.Data;
using LeveLEO.Features.Orders.DTO;
using LeveLEO.Features.Orders.Models;
using LeveLEO.Features.Products.DTO;
using LeveLEO.Infrastructure.Media.Services;
using LeveLEO.Infrastructure.Events;
using LeveLEO.Infrastructure.Events.DomainEvents;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Features.Orders.Services;

public class OrderItemReviewService(
    AppDbContext db,
    IMediaService mediaService,
    IEventBus eventBus) : IOrderItemReviewService
{
    #region Create & Update

    public async Task<ReviewResponseDto> CreateReviewAsync(string userId, CreateReviewDto dto)
    {
        // Валідація рейтингу
        if (dto.Rating < 1 || dto.Rating > 5)
        {
            throw new ApiException(
                "INVALID_RATING",
                "Rating must be between 1 and 5.",
                400
            );
        }

        var orderItem = await db.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
            .Include(oi => oi.Review)
            .FirstOrDefaultAsync(oi => oi.Id == dto.OrderItemId)
            ?? throw new ApiException(
                "ORDER_ITEM_NOT_FOUND",
                $"Order item with Id '{dto.OrderItemId}' not found.",
                404
            );

        if (orderItem.Order.UserId != userId)
        {
            throw new ApiException(
                "FORBIDDEN",
                "You can only review your own orders.",
                403
            );
        }

        if (orderItem.Order.Status != OrderStatus.Completed&& orderItem.Order.Status != OrderStatus.Shipped)
        {
            throw new ApiException(
                "ORDER_NOT_COMPLETED",
                "You can only review shipped or completed orders.",
                400
            );
        }

        if (orderItem.Review != null)
        {
            throw new ApiException(
                "REVIEW_ALREADY_EXISTS",
                "Review for this order item already exists.",
                400
            );
        }

        var review = new OrderItemReview
        {
            OrderItemId = dto.OrderItemId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            IsApproved = false, 
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        if (dto.PhotoKeys != null && dto.PhotoKeys.Count > 0)
        {
            foreach (var photoKey in dto.PhotoKeys)
            {
                review.Photos.Add(new OrderItemReviewPhoto
                {
                    PhotoKey = photoKey,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        if (dto.VideoKeys != null && dto.VideoKeys.Count > 0)
        {
            foreach (var videoKey in dto.VideoKeys)
            {
                review.Videos.Add(new OrderItemReviewVideo
                {
                    VideoKey = videoKey,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        db.OrderItemReviews.Add(review);
        await db.SaveChangesAsync();

        await db.Entry(review)
            .Reference(r => r.OrderItem)
            .Query()
            .Include(oi => oi.Product)
            .Include(oi => oi.Order)
                .ThenInclude(o => o.User)
            .LoadAsync();

        await eventBus.PublishAsync(new ReviewCreatedEvent
        {
            ReviewId = review.Id,
            OrderItemId = review.OrderItemId,
            ProductId = review.OrderItem.ProductId,
            UserId = review.OrderItem.Order.UserId,
            UserEmail = review.OrderItem.Order.User.Email!,
            Rating = review.Rating,
            ReviewText = review.Comment
        });

        return MapToDto(review);
    }

    public async Task<ReviewResponseDto> UpdateReviewAsync(string userId, Guid reviewId, UpdateReviewDto dto)
    {
        var review = await db.OrderItemReviews
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Order)
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Product)
            .Include(r => r.Photos)
            .Include(r => r.Videos)
            .FirstOrDefaultAsync(r => r.Id == reviewId)
            ?? throw new ApiException(
                "REVIEW_NOT_FOUND",
                $"Review with Id '{reviewId}' not found.",
                404
            );

        if (review.OrderItem.Order.UserId != userId)
        {
            throw new ApiException(
                "FORBIDDEN",
                "You can only update your own reviews.",
                403
            );
        }

        if (review.IsApproved)
        {
            throw new ApiException(
                "REVIEW_APPROVED",
                "Cannot edit approved review. Contact support if changes are needed.",
                400
            );
        }

        if (dto.Rating.HasValue)
        {
            if (dto.Rating.Value < 1 || dto.Rating.Value > 5)
            {
                throw new ApiException("INVALID_RATING", "Rating must be between 1 and 5.", 400);
            }
            review.Rating = dto.Rating.Value;
        }

        if (dto.Comment.HasValue)
        {
            review.Comment = dto.Comment.Value;
        }

        if (dto.PhotoKeys.HasValue)
        {

            foreach (var photo in review.Photos)
            {
                await mediaService.DeleteFileAsync(photo.PhotoKey);
            }

            db.OrderItemReviewPhotos.RemoveRange(review.Photos);
            review.Photos.Clear();

            if (dto.PhotoKeys.Value != null)
            {
                foreach (var photoKey in dto.PhotoKeys.Value)
                {
                    review.Photos.Add(new OrderItemReviewPhoto
                    {
                        PhotoKey = photoKey,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    });
                }
            }
        }

        if (dto.VideoKeys.HasValue)
        {
            foreach (var video in review.Videos)
            {
                await mediaService.DeleteFileAsync(video.VideoKey);
            }

            db.OrderItemReviewVideos.RemoveRange(review.Videos);
            review.Videos.Clear();

            if (dto.VideoKeys.Value != null)
            {
                foreach (var videoKey in dto.VideoKeys.Value)
                {
                    review.Videos.Add(new OrderItemReviewVideo
                    {
                        VideoKey = videoKey,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    });
                }
            }
        }

        review.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return MapToDto(review);
    }

    public async Task DeleteReviewAsync(string userId, Guid reviewId)
    {
        var review = await LoadReviewWithMediaForDeleteAsync(reviewId);

        if (review.OrderItem.Order.UserId != userId)
        {
            throw new ApiException("FORBIDDEN", "You can only delete your own reviews.", 403);
        }

        await DeleteReviewCoreAsync(review);
    }

    public async Task DeleteReviewAsModeratorAsync(Guid reviewId)
    {
        var review = await LoadReviewWithMediaForDeleteAsync(reviewId);
        await DeleteReviewCoreAsync(review);
    }

    private async Task<OrderItemReview> LoadReviewWithMediaForDeleteAsync(Guid reviewId)
    {
        return await db.OrderItemReviews
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Order)
            .Include(r => r.Photos)
            .Include(r => r.Videos)
            .FirstOrDefaultAsync(r => r.Id == reviewId)
            ?? throw new ApiException(
                "REVIEW_NOT_FOUND",
                $"Review with Id '{reviewId}' not found.",
                404
            );
    }

    private async Task DeleteReviewCoreAsync(OrderItemReview review)
    {
        foreach (var photo in review.Photos)
        {
            await mediaService.DeleteFileAsync(photo.PhotoKey);
        }

        foreach (var video in review.Videos)
        {
            await mediaService.DeleteFileAsync(video.VideoKey);
        }

        db.OrderItemReviews.Remove(review);
        await db.SaveChangesAsync();
    }

    #endregion Create & Update

    #region Get Reviews

    public async Task<ReviewResponseDto> GetReviewByIdAsync(Guid reviewId)
    {
        var review = await db.OrderItemReviews
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Product)
            .Include(r => r.Photos)
            .Include(r => r.Videos)
            .FirstOrDefaultAsync(r => r.Id == reviewId)
            ?? throw new ApiException(
                "REVIEW_NOT_FOUND",
                $"Review with Id '{reviewId}' not found.",
                404
            );

        return MapToDto(review);
    }

    public async Task<List<ReviewResponseDto>> GetUserReviewsAsync(string userId)
    {
        var reviews = await db.OrderItemReviews
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Order)
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Product)
            .Include(r => r.Photos)
            .Include(r => r.Videos)
            .Where(r => r.OrderItem.Order.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return [.. reviews.Select(MapToDto)];
    }

    public async Task<ReviewResponseDto?> GetReviewByOrderItemAsync(Guid orderItemId)
    {
        var review = await db.OrderItemReviews
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Product)
            .Include(r => r.Photos)
            .Include(r => r.Videos)
            .FirstOrDefaultAsync(r => r.OrderItemId == orderItemId);

        return review != null ? MapToDto(review) : null;
    }

    public async Task<ProductReviewsDto> GetProductReviewsAsync(
        Guid productId,
        int page = 1,
        int pageSize = 10,
        bool approvedOnly = true)
    {
        var query = db.OrderItemReviews
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Product)
            .Include(r => r.Photos)
            .Include(r => r.Videos)
            .Where(r => r.OrderItem.ProductId == productId);

        if (approvedOnly)
        {
            query = query.Where(r => r.IsApproved);
        }

        var totalReviews = await query.CountAsync();

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var ratingCounts = await db.OrderItemReviews
            .Where(r => r.OrderItem.ProductId == productId && (!approvedOnly || r.IsApproved))
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync();

        var averageRating = reviews.Count != 0
            ? reviews.Average(r => (decimal)r.Rating)
            : 0m;

        return new ProductReviewsDto
        {
            ProductId = productId,
            AverageRating = averageRating,
            TotalReviews = totalReviews,
            FiveStarCount = ratingCounts.FirstOrDefault(r => r.Rating == 5)?.Count ?? 0,
            FourStarCount = ratingCounts.FirstOrDefault(r => r.Rating == 4)?.Count ?? 0,
            ThreeStarCount = ratingCounts.FirstOrDefault(r => r.Rating == 3)?.Count ?? 0,
            TwoStarCount = ratingCounts.FirstOrDefault(r => r.Rating == 2)?.Count ?? 0,
            OneStarCount = ratingCounts.FirstOrDefault(r => r.Rating == 1)?.Count ?? 0,
            Reviews = [.. reviews.Select(MapToDto)]
        };
    }

    #endregion Get Reviews

    #region Admin Actions

    public async Task<ReviewResponseDto> ApproveReviewAsync(Guid reviewId)
    {
        var review = await db.OrderItemReviews
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Product)
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Order)
                    .ThenInclude(o => o.User)
            .Include(r => r.Photos)
            .Include(r => r.Videos)
            .FirstOrDefaultAsync(r => r.Id == reviewId)
            ?? throw new ApiException(
                "REVIEW_NOT_FOUND",
                $"Review with Id '{reviewId}' not found.",
                404
            );

        review.IsApproved = true;
        review.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        await eventBus.PublishAsync(new ReviewApprovedEvent
        {
            ReviewId = review.Id,
            ProductId = review.OrderItem.ProductId,
            UserId = review.OrderItem.Order.UserId,
            UserEmail = review.OrderItem.Order.User.Email!,
            Rating = review.Rating
        });

        return MapToDto(review);
    }

    public async Task<ReviewResponseDto> RejectReviewAsync(Guid reviewId)
    {
        var review = await db.OrderItemReviews
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Product)
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Order)
                    .ThenInclude(o => o.User)
            .Include(r => r.Photos)
            .Include(r => r.Videos)
            .FirstOrDefaultAsync(r => r.Id == reviewId)
            ?? throw new ApiException(
                "REVIEW_NOT_FOUND",
                $"Review with Id '{reviewId}' not found.",
                404
            );

        review.IsApproved = false;
        review.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        await eventBus.PublishAsync(new ReviewRejectedEvent
        {
            ReviewId = review.Id,
            UserId = review.OrderItem.Order.UserId,
            UserEmail = review.OrderItem.Order.User.Email!
        });

        return MapToDto(review);
    }

    public async Task<PagedResultDto<ReviewResponseDto>> GetPendingReviewsAsync(int page = 1, int pageSize = 20)
    {
        var query = db.OrderItemReviews
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Product)
            .Include(r => r.Photos)
            .Include(r => r.Videos)
            .Where(r => !r.IsApproved)
            .OrderBy(r => r.CreatedAt);

        var totalCount = await query.CountAsync();

        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<ReviewResponseDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = [.. reviews.Select(MapToDto)]
        };
    }

    public async Task<PagedResultDto<ReviewResponseDto>> GetAllReviewsAsync(int page = 1, int pageSize = 20)
    {
        var query = db.OrderItemReviews
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Product)
            .Include(r => r.Photos)
            .Include(r => r.Videos)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();

        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<ReviewResponseDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = [.. reviews.Select(MapToDto)]
        };
    }

    public async Task<List<ReviewResponseDto>> GetFeaturedReviewsForHomepageAsync()
    {
        var reviews = await db.OrderItemReviews
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Product)
            .Include(r => r.Photos)
            .Include(r => r.Videos)
            .Where(r => r.IsApproved && r.Rating == 5 && r.OrderItem.Product.IsActive && r.OrderItem.Product.StockQuantity > 0)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync();

        return [.. reviews.Select(MapToDto)];
    }

    #endregion Admin Actions

    #region Helpers

    private static ReviewResponseDto MapToDto(OrderItemReview review)
    {
        return new ReviewResponseDto
        {
            Id = review.Id,
            OrderItemId = review.OrderItemId,
            ProductId = review.OrderItem.ProductId,
            ProductName = review.OrderItem.Product.Name,
            Rating = review.Rating,
            Comment = review.Comment,
            IsApproved = review.IsApproved,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt,
            Photos = [.. review.Photos.Select(p => new ReviewPhotoDto
            {
                Id = p.Id,
                PhotoKey = p.PhotoKey
            })],
            Videos = [.. review.Videos.Select(v => new ReviewVideoDto
            {
                Id = v.Id,
                VideoKey = v.VideoKey
            })]
        };
    }

    #endregion Helpers
}
