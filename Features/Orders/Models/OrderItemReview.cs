using LeveLEO.Features.Identity.Models;
using LeveLEO.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeveLEO.Features.Orders.Models;

public class OrderItemReview : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderItemId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public OrderItem OrderItem { get; set; } = null!;
    public ICollection<OrderItemReviewPhoto> Photos { get; set; } = [];
    public ICollection<OrderItemReviewVideo> Videos { get; set; } = [];
}