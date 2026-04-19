using LeveLEO.Data;
using LeveLEO.Features.Orders.Models;
using LeveLEO.Features.Orders.Services;
using LeveLEO.Features.Payments.DTO;
using LeveLEO.Features.Payments.Models;
using LeveLEO.Features.Products.DTO;
using LeveLEO.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeveLEO.Features.Payments.Services;

public class PaymentService(
    AppDbContext db,
    ILiqPayService liqPayService,
    IServiceProvider serviceProvider,
    ILogger<PaymentService> logger) : IPaymentService
{
    private static readonly JsonSerializerOptions s_liqPayPayloadJson = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

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
        var signature = liqPayService.GenerateSignature(payload);

        return new CreatePaymentResultDto
        {
            Payload = payload,
            Signature = signature,
            ExpireAt = expireAt,
            PaymentId = payment.Id
        };
    }

    public async Task<PagedResultDto<PaymentListItemDto>> GetAllPaymentsAsync(AdminPaymentFilterDto filter)
    {
        var query = db.Payments
            .Include(p => p.Order)
            .AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(p => p.Status == filter.Status.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(p => p.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(p => p.CreatedAt <= filter.EndDate.Value);

        query = filter.SortBy?.ToLower() switch
        {
            "amount" => filter.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(p => p.Amount)
                : query.OrderByDescending(p => p.Amount),
            "status" => filter.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(p => p.Status)
                : query.OrderByDescending(p => p.Status),
            "expireat" => filter.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(p => p.ExpireAt)
                : query.OrderByDescending(p => p.ExpireAt),
            _ => filter.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(p => p.CreatedAt)
                : query.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(p => new PaymentListItemDto
            {
                Id = p.Id,
                OrderId = p.OrderId,
                OrderNumber = p.Order.Number,
                Amount = p.Amount,
                Currency = p.Currency,
                LiqPayPaymentId = p.LiqPayPaymentId,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                ExpireAt = p.ExpireAt
            })
            .ToListAsync();

        return new PagedResultDto<PaymentListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
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
        var wasPending = payment.Status == PaymentStatus.Pending;
        if (wasPending)
        {
            try
            {
                var liqStatus = await liqPayService.GetPaymentStatusAsync(payment.Id);
                ApplyLiqPayStatusToPayment(payment, liqStatus);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "LiqPay status sync failed for payment {PaymentId}; returning DB state.", paymentId);
            }
        }

        if (wasPending && payment.Status != PaymentStatus.Pending)
        {
            var orderService = serviceProvider.GetRequiredService<IOrderService>();
            await orderService.NotifyPaymentUpdated(payment.Id);
        }

        return MapToDto(payment);
    }

    public async Task HandleLiqPayCallbackAsync(LiqPayStatusResponseDto callback)
    {
        var orderId = callback.OrderId
            ?? throw new ApiException("INVALID_LIQPAY_PAYLOAD", "LiqPay callback missing order_id.", 422);

        var payment = await db.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id.ToString() == orderId)
            ?? throw new ApiException(
        "PAYMENT_NOT_FOUND",
        "Payment not found.",
        404
    );

        ApplyLiqPayStatusToPayment(payment, callback);

        await db.SaveChangesAsync();

        if (payment.Status != PaymentStatus.Pending)
        {
            var orderService = serviceProvider.GetRequiredService<IOrderService>();
            await orderService.NotifyPaymentUpdated(payment.Id);
        }
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

        var callback = JsonSerializer.Deserialize<LiqPayStatusResponseDto>(json, s_liqPayPayloadJson)
            ?? throw new ApiException(
                "INVALID_LIQPAY_PAYLOAD",
                "Invalid LiqPay callback payload.",
                422
            );

        return callback;
    }

    private static void ApplyLiqPayStatusToPayment(Payment payment, LiqPayStatusResponseDto liq)
    {
        var raw = liq.Status?.Trim().ToLowerInvariant();
        payment.Status = raw switch
        {
            "success" or "sandbox" => PaymentStatus.Success,
            "failure" or "error" => PaymentStatus.Failure,
            "reversed" => PaymentStatus.Refunded,
            "pending" or "wait_accept" => PaymentStatus.Pending,
            _ => PaymentStatus.Pending
        };

        if (liq.PaymentId.HasValue)
            payment.LiqPayPaymentId = liq.PaymentId.Value.ToString();

        payment.UpdatedAt = DateTimeOffset.UtcNow;
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

        var orderService = serviceProvider.GetRequiredService<IOrderService>();
        await orderService.NotifyPaymentUpdated(paymentId);
    }

    public async Task RefundPaymentAsync(Guid paymentId, decimal? amount = null, string? reason = null)
    {
        var payment = await db.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new ApiException("PAYMENT_NOT_FOUND", $"Payment with Id '{paymentId}' not found.", 404);

        if (payment.Status != PaymentStatus.Success)
            throw new ApiException("PAYMENT_STATUS_IS_NOT_SUCCESSFUL", "Only successful payments can be refunded", 404);

        // LiqPayService для рефанду
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
        CreatedAt = payment.CreatedAt,
        UpdatedAt = payment.UpdatedAt
    };
}
