using LeveLEO.Features.Orders.DTO;
using LeveLEO.Features.Orders.Models;
using LeveLEO.Features.Products.DTO;
using LeveLEO.Features.Shipping.Models;

namespace LeveLEO.Features.Orders.Services;

public interface IOrderService
{
    Task<OrderDetailDto> GetByIdAsync(Guid orderId);

    Task<OrderDetailDto> GetByNumberAsync(string orderNumber);

    Task<List<OrderListItemDto>> GetByUserIdAsync(string userId, DateTimeOffset? startDate, DateTimeOffset? endDate);

    Task<PagedResultDto<OrderListItemDto>> GetAllOrdersAsync(AdminOrderFilterDto filter);

    Task<CreateOrderResultDto> CreateOrderFromCartAsync(string userId, OrderCreateDto orderCreateDto, string serverUrl);

    Task<OrderDetailDto> UpdateAsync(Guid orderId, OrderUpdateDto order, string requestingUserId, bool isStaff);

    Task NotifyPaymentUpdated(Guid paymentId);

    Task NotifyDeliveryUpdated(Guid orderId, DeliveryStatus deliveryStatus);

    /// <summary>Сканує прострочені неоплачені платежі (фоновий сервіс + можна викликати вручну).</summary>
    Task ProcessExpiredPendingPaymentsAsync(CancellationToken cancellationToken = default);
}