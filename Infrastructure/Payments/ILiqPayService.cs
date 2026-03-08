using LeveLEO.Features.Payments.Models;

namespace LeveLEO.Infrastructure.Payments;

public interface ILiqPayService
{
    string GetPublicKey();

    string GenerateData(Payment payment, string serverUrl, DateTimeOffset expireAt);

    string GenerateSignature(string data);

    LiqPayStatusResponseDto VerifyAndParseCallback(string data, string signature);

    Task RefundPaymentAsync(string orderId, decimal? amount = null, string? reason = null);

    Task<LiqPayStatusResponseDto> GetPaymentStatusAsync(Guid orderId);
}