using System.Timers;

namespace LeveLEO.Infrastructure.Payments;

public class LiqPayStatusResponseDto
{
    public string Status { get; set; } = null!;
    public string OrderId { get; set; } = null!;
    public string PaymentId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public long? Time { get; set; }

    public DateTime? CreatedAt => Time.HasValue
      ? DateTimeOffset.FromUnixTimeSeconds(Time.Value).UtcDateTime
      : null;
}