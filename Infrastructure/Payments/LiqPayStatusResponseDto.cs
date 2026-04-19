using System.Text.Json.Serialization;

namespace LeveLEO.Infrastructure.Payments;

/// <summary>
/// Фрагмент відповіді LiqPay (callback / status). Поля в JSON — зазвичай snake_case.
/// </summary>
public class LiqPayStatusResponseDto
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    /// <summary>LiqPay надсилає число; для refund потрібен цей id у рядку.</summary>
    [JsonPropertyName("payment_id")]
    public long? PaymentId { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("create_date")]
    public long? CreateDate { get; set; }

    public DateTime? CreatedAt => CreateDate.HasValue
        ? DateTimeOffset.FromUnixTimeSeconds(CreateDate.Value).UtcDateTime
        : null;
}