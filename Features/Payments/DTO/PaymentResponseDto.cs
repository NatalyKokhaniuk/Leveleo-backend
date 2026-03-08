using LeveLEO.Features.Orders.Models;
using LeveLEO.Features.Payments.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.Payments.DTO;

public class PaymentResponseDto : ITimestamped

{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UAH";
    public string? LiqPayPaymentId { get; set; } = null!;

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTime.UtcNow;
}