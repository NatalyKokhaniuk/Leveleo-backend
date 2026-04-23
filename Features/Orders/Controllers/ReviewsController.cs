// Features/Orders/Controllers/ReviewsController.cs
using LeveLEO.Features.Orders.DTO;
using LeveLEO.Features.Orders.Services;
using LeveLEO.Features.Products.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LeveLEO.Features.Orders.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController(IOrderItemReviewService reviewService) : ControllerBase
{
    #region User Actions

    /// <summary>
    /// Створити відгук для OrderItem
    /// Доступно: авторизовані користувачі для своїх завершених замовлень
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReviewResponseDto>> CreateReview([FromBody] CreateReviewDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await reviewService.CreateReviewAsync(userId, dto);
        return CreatedAtAction(nameof(GetReviewById), new { reviewId = result.Id }, result);
    }

    /// <summary>
    /// Оновити свій відгук (тільки не схвалені)
    /// </summary>
    [HttpPut("{reviewId:guid}")]
    [Authorize]
    public async Task<ActionResult<ReviewResponseDto>> UpdateReview(
        Guid reviewId,
        [FromBody] UpdateReviewDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await reviewService.UpdateReviewAsync(userId, reviewId, dto);
        return Ok(result);
    }

    /// <summary>
    /// Видалити відгук: власник — свій відгук; Admin/Moderator — будь-який (у т.ч. схвалений)
    /// </summary>
    [HttpDelete("{reviewId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(Guid reviewId)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Moderator"))
        {
            await reviewService.DeleteReviewAsModeratorAsync(reviewId);
        }
        else
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await reviewService.DeleteReviewAsync(userId, reviewId);
        }

        return NoContent();
    }

    /// <summary>
    /// Отримати всі свої відгуки
    /// </summary>
    [HttpGet("my-reviews")]
    [Authorize]
    public async Task<ActionResult<List<ReviewResponseDto>>> GetMyReviews()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var reviews = await reviewService.GetUserReviewsAsync(userId);
        return Ok(reviews);
    }

    #endregion User Actions

    #region Public Access

    /// <summary>
    /// Отримати відгук за ID
    /// Доступно: всім (тільки схвалені для неавторизованих)
    /// </summary>
    [HttpGet("{reviewId:guid}")]
    public async Task<ActionResult<ReviewResponseDto>> GetReviewById(Guid reviewId)
    {
        var review = await reviewService.GetReviewByIdAsync(reviewId);

        // Якщо не авторизований і відгук не схвалено - заборонити
        if (!User.Identity?.IsAuthenticated == true && !review.IsApproved)
        {
            return NotFound();
        }

        // Якщо авторизований, але не власник і не схвалено - заборонити
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("Moderator");

        if (!review.IsApproved && !isAdmin)
        {
            // Перевірити, чи це власний відгук через OrderItem
            var userReviews = await reviewService.GetUserReviewsAsync(userId!);
            if (!userReviews.Any(r => r.Id == reviewId))
            {
                return NotFound();
            }
        }

        return Ok(review);
    }

    /// <summary>
    /// Отримати відгук для конкретного OrderItem
    /// Доступно: власник замовлення або публічно якщо схвалено
    /// </summary>
    [HttpGet("order-item/{orderItemId:guid}")]
    public async Task<ActionResult<ReviewResponseDto>> GetReviewByOrderItem(Guid orderItemId)
    {
        var review = await reviewService.GetReviewByOrderItemAsync(orderItemId);

        if (review == null)
        {
            return NotFound();
        }

        // Перевірка доступу аналогічно GetReviewById
        if (!User.Identity?.IsAuthenticated == true && !review.IsApproved)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("Moderator");

        if (!review.IsApproved && !isAdmin)
        {
            var userReviews = await reviewService.GetUserReviewsAsync(userId!);
            if (!userReviews.Any(r => r.Id == review.Id))
            {
                return NotFound();
            }
        }

        return Ok(review);
    }

    /// <summary>
    /// Отримати всі відгуки для продукту
    /// Доступно: всім (тільки схвалені)
    /// </summary>
    [HttpGet("product/{productId:guid}")]
    public async Task<ActionResult<ProductReviewsDto>> GetProductReviews(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await reviewService.GetProductReviewsAsync(productId, page, pageSize, approvedOnly: true);
        return Ok(result);
    }

    /// <summary>
    /// Отримати топ-5 п'ятизіркових відгуків для товарів в наявності (для головної сторінки)
    /// Доступно: всім
    /// </summary>
    [HttpGet("homepage-featured")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ReviewResponseDto>>> GetFeaturedReviews()
    {
        var reviews = await reviewService.GetFeaturedReviewsForHomepageAsync();
        return Ok(reviews);
    }

    #endregion Public Access

    #region Admin Actions

    /// <summary>
    /// Схвалити відгук
    /// Доступно: тільки адміністратори
    /// </summary>
    [HttpPost("{reviewId:guid}/approve")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ReviewResponseDto>> ApproveReview(Guid reviewId)
    {
        var result = await reviewService.ApproveReviewAsync(reviewId);
        return Ok(result);
    }

    /// <summary>
    /// Відхилити відгук
    /// Доступно: тільки адміністратори
    /// </summary>
    [HttpPost("{reviewId:guid}/reject")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ReviewResponseDto>> RejectReview(Guid reviewId)
    {
        var result = await reviewService.RejectReviewAsync(reviewId);
        return Ok(result);
    }

    /// <summary>
    /// Отримати всі не схвалені відгуки (для модерації)
    /// Доступно: Admin, Moderator
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<PagedResultDto<ReviewResponseDto>>> GetPendingReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await reviewService.GetPendingReviewsAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Отримати всі відгуки (схвалені та на модерації), найновіші спочатку
    /// Доступно: Admin, Moderator
    /// </summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<PagedResultDto<ReviewResponseDto>>> GetAllReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await reviewService.GetAllReviewsAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Отримати всі відгуки конкретного користувача
    /// Доступно: тільки адміністратори
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<List<ReviewResponseDto>>> GetUserReviews(string userId)
    {
        var reviews = await reviewService.GetUserReviewsAsync(userId);
        return Ok(reviews);
    }

    /// <summary>
    /// Отримати всі відгуки для продукту (включно з не схваленими)
    /// Доступно: тільки адміністратори
    /// </summary>
    [HttpGet("admin/product/{productId:guid}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ProductReviewsDto>> GetAllProductReviews(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await reviewService.GetProductReviewsAsync(productId, page, pageSize, approvedOnly: false);
        return Ok(result);
    }

    #endregion Admin Actions
}