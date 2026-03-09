using Amazon.S3.Model;
using LeveLEO.Data;
using LeveLEO.Features.Orders.Models;
using LeveLEO.Features.Orders.Services;
using LeveLEO.Features.Payments.DTO;
using LeveLEO.Features.Payments.Models;
using LeveLEO.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LeveLEO.Features.Payments.Services;

public class PaymentService(AppDbContext db, ILiqPayService liqPayService) : IPaymentService
{
    public async Task<CreatePaymentResultDto> CreatePaymentAsync(
    Order order,
    TimeSpan payloadValidity,
    string serverUrl)
    {
        var expireAt = DateTimeOffset.UtcNow.Add(payloadValidity);

        var payment = new Payment
        {
            OrderId = order.Id,
            Amount = order.TotalPayable,
            Currency = "UAH",
            Status = PaymentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            ExpireAt = expireAt
        };

        await db.Payments.AddAsync(payment);
        await db.SaveChangesAsync();

        var payload = liqPayService.GenerateData(payment, serverUrl, expireAt);

        return new CreatePaymentResultDto
        {
            Payload = payload,
            ExpireAt = expireAt,
            PaymentId = payment.Id
        };
    }

    public async Task<PaymentResponseDto> GetPaymentByIdAsync(Guid paymentId)
    {
        var payment = await db.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new ApiException(
        "PAYMENT_NOT_FOUND",
        "Payment not found.",
        404
    );
        if (payment != null
            && payment.Status == PaymentStatus.Pending
            && payment.ExpireAt <= DateTimeOffset.UtcNow)
        {
            var liqStatus = await liqPayService.GetPaymentStatusAsync(payment.Id);

            payment.Status = liqStatus.Status switch
            {
                "success" => PaymentStatus.Success,
                "failure" => PaymentStatus.Failure,
                "pending" => PaymentStatus.Pending,
                _ => PaymentStatus.Pending
            };

            payment.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }
        if (payment != null)
            return MapToDto(payment);
        throw new ApiException("PAYMENT_NOT_FOUND", $"Payment with id {paymentId} not found.", 404);
    }

    public async Task HandleLiqPayCallbackAsync(LiqPayStatusResponseDto callback)
    {
        // Отримуємо Payment по LiqPayPaymentId
        var payment = await db.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id.ToString() == callback.OrderId)
            ?? throw new ApiException(
        "PAYMENT_NOT_FOUND",
        "Payment not found.",
        404
    );

        // Оновлюємо статус
        payment.Status = callback.Status switch
        {
            "success" => PaymentStatus.Success,
            "failure" => PaymentStatus.Failure,
            "pending" => PaymentStatus.Success,//для тестування, потім поміняти

            _ => PaymentStatus.Pending,
        };

        await db.SaveChangesAsync();
    }

    public LiqPayStatusResponseDto VerifyCallback(string data, string signature)
    {
        var expectedSignature = liqPayService.GenerateSignature(data);

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

    public async Task CancelPaymentAsync(Guid paymentId)
    {
        var payment = await db.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new ApiException(
        "PAYMENT_NOT_FOUND",
        "Payment not found.",
        404);

        if (payment.Status == PaymentStatus.Success)
            throw new ApiException(
        "CANNOT_CANCE_PAYMENT",
        "Cannot cancel a successful payment. Use RefundPaymentAsync instead.", 422);

        payment.Status = PaymentStatus.Failure;

        await db.SaveChangesAsync();
    }

    public async Task RefundPaymentAsync(Guid paymentId, decimal? amount = null, string? reason = null)
    {
        var payment = await db.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new ApiException("PAYMENT_NOT_FOUND", $"Payment with Id '{paymentId}' not found.", 404);

        if (payment.Status != PaymentStatus.Success)
            throw new ApiException("PAYMENT_STATUS_IS_NOT_SUCCESSFUL", "Only successful payments can be refunded", 404);

        // Викликаємо LiqPayService для рефанду
        await liqPayService.RefundPaymentAsync(payment.LiqPayPaymentId!, amount, reason);

        payment.Status = PaymentStatus.Refunded;

        await db.SaveChangesAsync();
    }

    private static PaymentResponseDto MapToDto(Payment payment) => new()
    {
        Id = payment.Id,
        OrderId = payment.OrderId,
        Amount = payment.Amount,
        Currency = payment.Currency,
        LiqPayPaymentId = payment.LiqPayPaymentId,
        Status = payment.Status,
    };
}