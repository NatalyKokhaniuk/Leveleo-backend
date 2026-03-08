using LeveLEO.Features.Payments.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LeveLEO.Infrastructure.Payments;

public class LiqPayService(IConfiguration config, HttpClient httpClient) : ILiqPayService
{
    private readonly string publicKey =
        config["LiqPay:PublicKey"]
        ?? throw new InvalidOperationException("LiqPay public key is not configured.");

    private readonly string privateKey =
        config["LiqPay:PrivateKey"]
        ?? throw new InvalidOperationException("LiqPay private key is not configured.");

    public string GetPublicKey() => publicKey;

    public string GenerateData(Payment payment, string serverUrl, DateTimeOffset expireAt)
    {
        var frontendUrl =
        config["Frontend:Url"]
        ?? throw new InvalidOperationException("Frontend Url is not configured.");
        var sandboxMode = config.GetValue<bool>("LiqPay:SandboxMode");
        var payload = new
        {
            public_key = publicKey,
            action = "pay",
            version = "3",
            amount = payment.Amount,
            currency = payment.Currency,
            description = $"Order payment in LeveLEO store #{payment.OrderId}",
            order_id = payment.Id.ToString(),
            sandbox = sandboxMode ? 1 : 0,
            server_url = $"{serverUrl}/api/payments/callback",
            result_url = $"{frontendUrl}/order-success?orderId={payment.OrderId}",
            expired_date = expireAt.ToUnixTimeSeconds()
        };

        var json = JsonSerializer.Serialize(payload);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public string GenerateSignature(string data)
    {
        var signString = privateKey + data + privateKey;
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(signString));
        return Convert.ToBase64String(hash);
    }

    public LiqPayStatusResponseDto VerifyAndParseCallback(string data, string signature)
    {
        var expectedSignature = GenerateSignature(data);

        if (expectedSignature != signature)
            throw new ApiException(
                "INVALID_LIQPAY_SIGNATURE",
                "Invalid LiqPay callback signature.",
                422
            );

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(data));

        var callback = JsonSerializer.Deserialize<LiqPayStatusResponseDto>(json)
            ?? throw new ApiException(
                "INVALID_LIQPAY_PAYLOAD",
                "Invalid LiqPay callback payload.",
                422
            );

        return callback;
    }

    public async Task RefundPaymentAsync(string orderId, decimal? amount = null, string? reason = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["public_key"] = publicKey,
            ["action"] = "refund",
            ["version"] = "3",
            ["order_id"] = orderId
        };

        if (amount.HasValue)
            payload["amount"] = amount.Value;

        if (!string.IsNullOrWhiteSpace(reason))
            payload["description"] = reason;

        var json = JsonSerializer.Serialize(payload);
        var data = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var signature = GenerateSignature(data);

        var response = await httpClient.PostAsJsonAsync(
            "https://www.liqpay.ua/api/request",
            new { data, signature });

        if (!response.IsSuccessStatusCode)
            throw new ApiException(
                "LIQPAY_HTTP_ERROR",
                $"LiqPay HTTP error: {(int)response.StatusCode}",
                (int)response.StatusCode
            );

        var rawResponse = await response.Content.ReadAsStringAsync();

        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(rawResponse)
            ?? throw new ApiException(
                "LIQPAY_INVALID_RESPONSE",
                "Invalid LiqPay response format.",
                422
            );

        if (!parsed.TryGetValue("data", out var responseDataBase64))
            throw new ApiException(
                "LIQPAY_NO_DATA",
                "LiqPay response does not contain data field.",
                404
            );

        if (!parsed.TryGetValue("signature", out var responseSignature))
            throw new ApiException(
                "LIQPAY_NO_SIGNATURE",
                "LiqPay response does not contain signature.",
                404
            );

        var expectedSignature = GenerateSignature(responseDataBase64);
        if (expectedSignature != responseSignature)
            throw new ApiException(
                "LIQPAY_INVALID_SIGNATURE",
                "Invalid LiqPay refund response signature.",
                422
            );

        var decodedJson = Encoding.UTF8.GetString(
            Convert.FromBase64String(responseDataBase64));

        var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(decodedJson)
            ?? throw new ApiException(
                "LIQPAY_INVALID_REFUND_PAYLOAD",
                "Invalid LiqPay refund payload.",
                422
            );

        if (!responseData.TryGetValue("status", out var statusObj))
            throw new ApiException(
                "LIQPAY_NO_STATUS",
                "Refund response does not contain status.",
                422
            );

        var status = statusObj?.ToString();

        if (status != "success" && status != "pending")
            throw new ApiException(
                "REFUND_FAILED",
                $"Refund failed with status: {status}",
                422
            );
    }

    public async Task<LiqPayStatusResponseDto> GetPaymentStatusAsync(Guid orderId)
    {
        var payload = new
        {
            public_key = publicKey,
            action = "status",
            version = "3",
            order_id = orderId.ToString()
        };

        var json = JsonSerializer.Serialize(payload);
        var data = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var signature = GenerateSignature(data);

        var response = await httpClient.PostAsJsonAsync(
            "https://www.liqpay.ua/api/request",
            new { data, signature });

        if (!response.IsSuccessStatusCode)
            throw new ApiException(
                "LIQPAY_HTTP_ERROR",
                $"LiqPay HTTP error: {(int)response.StatusCode}",
                (int)response.StatusCode
            );

        var rawResponse = await response.Content.ReadAsStringAsync();

        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(rawResponse)
            ?? throw new ApiException(
                "LIQPAY_INVALID_RESPONSE",
                "Invalid LiqPay response format.",
                422
            );

        if (!parsed.TryGetValue("data", out var responseDataBase64))
            throw new ApiException(
                "LIQPAY_NO_DATA",
                "LiqPay response does not contain data field.",
                404
            );

        if (!parsed.TryGetValue("signature", out var responseSignature))
            throw new ApiException(
                "LIQPAY_NO_SIGNATURE",
                "LiqPay response does not contain signature.",
                404
            );

        var expectedSignature = GenerateSignature(responseDataBase64);
        if (expectedSignature != responseSignature)
            throw new ApiException(
                "LIQPAY_INVALID_SIGNATURE",
                "Invalid LiqPay response signature.",
                422
            );

        var decodedJson = Encoding.UTF8.GetString(
            Convert.FromBase64String(responseDataBase64));

        var result = JsonSerializer.Deserialize<LiqPayStatusResponseDto>(decodedJson)
            ?? throw new ApiException(
                "LIQPAY_INVALID_STATUS_PAYLOAD",
                "Invalid LiqPay status payload.",
                422
            );

        return result;
    }
}