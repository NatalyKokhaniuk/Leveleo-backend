using LeveLEO.Features.Identity.Models;
using LeveLEO.Features.Payments.Models;
using LeveLEO.Features.Shipping.Models;
using LeveLEO.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeveLEO.Features.Orders.Models;

public class Order : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public Guid AddressId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<OrderItem> OrderItems { get; set; } = [];
    public decimal TotalOriginalPrice { get; set; }
    public decimal TotalProductDiscount { get; set; } = 0.00m;
    public decimal TotalCartDiscount { get; set; } = 0.00m;

    public decimal TotalPayable { get; set; }

    /// <summary>Застосована акція рівня кошика на момент оформлення (для нарахування використань купона після оплати).</summary>
    public Guid? AppliedCartPromotionId { get; set; }

    public Guid? DeliveryId { get; set; }
    public Delivery? Delivery { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public Address Address { get; set; } = null!;
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Completed,
    Cancelled,
    PaymentFailed
}