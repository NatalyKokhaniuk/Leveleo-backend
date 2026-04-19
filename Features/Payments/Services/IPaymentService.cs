using LeveLEO.Features.Orders.Models;
using LeveLEO.Features.Payments.DTO;
using LeveLEO.Features.Payments.Models;
using LeveLEO.Features.Products.DTO;
using LeveLEO.Infrastructure.Payments;

namespace LeveLEO.Features.Payments.Services;

public interface IPaymentService
{
    Task<CreatePaymentResultDto> CreatePaymentAsync(Order order,
    TimeSpan payloadValidity,
    string serverUrl);

    Task<PagedResultDto<PaymentListItemDto>> GetAllPaymentsAsync(AdminPaymentFilterDto filter);

    Task<PaymentResponseDto> GetPaymentByIdAsync(Guid paymentId);

    Task HandleLiqPayCallbackAsync(LiqPayStatusResponseDto callback);

    Task CancelPaymentAsync(Guid paymentId);

    Task RefundPaymentAsync(Guid paymentId, decimal? amount = null, string? reason = null);

    LiqPayStatusResponseDto VerifyCallback(string data, string signature);
}