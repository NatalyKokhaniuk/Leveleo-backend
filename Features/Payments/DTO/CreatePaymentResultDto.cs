namespace LeveLEO.Features.Payments.DTO;

public class CreatePaymentResultDto
{
    /// <summary>Base64-encoded JSON for LiqPay form field <c>data</c> (same value the client posts to checkout).</summary>
    public string Payload { get; set; } = null!;

    /// <summary>Base64 SHA-1 signature for LiqPay form field <c>signature</c>.</summary>
    public string Signature { get; set; } = null!;

    public DateTimeOffset ExpireAt { get; set; }
    public Guid PaymentId { get; set; }
}