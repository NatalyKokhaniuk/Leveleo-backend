using LeveLEO.Models;

namespace LeveLEO.Features.Orders.Models;

public class OrderItemReviewPhoto : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderItemReviewId { get; set; }
    public string PhotoKey { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public OrderItemReview OrderItemReview { get; set; } = null!;
}