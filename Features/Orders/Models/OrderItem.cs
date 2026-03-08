using LeveLEO.Features.Identity.Models;
using LeveLEO.Features.Products.Models;
using LeveLEO.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeveLEO.Features.Orders.Models;

public class OrderItem : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountedUnitPrice { get; set; }

    [NotMapped]
    public decimal TotalOriginalPrice => Quantity * UnitPrice;

    [NotMapped]
    public decimal TotalDiscountedPrice => Quantity * DiscountedUnitPrice;

    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public OrderItemReview? Review { get; set; }
}