using LeveLEO.Features.Payments.Models;

namespace LeveLEO.Features.Payments.DTO;

/// <summary>
/// Елемент списку платежів (адмін-панель).
/// </summary>
public class PaymentListItemDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UAH";
    public string? LiqPayPaymentId { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset ExpireAt { get; set; }
}
