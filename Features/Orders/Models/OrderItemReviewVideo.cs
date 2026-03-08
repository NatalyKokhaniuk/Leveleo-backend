using LeveLEO.Models;

namespace LeveLEO.Features.Orders.Models;

public class OrderItemReviewVideo : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderItemReviewId { get; set; }
    public string VideoKey { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTime.UtcNow;
    public OrderItemReview OrderItemReview { get; set; } = null!;
}