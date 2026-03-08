using LeveLEO.Features.Orders.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.Payments.Models;

public class Payment : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UAH";
    public string? LiqPayPaymentId { get; set; } = null!; // від LiqPay

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset ExpireAt { get; set; }
}

public enum PaymentStatus
{
    Pending,
    Success,
    Failure,
    Refunded
}