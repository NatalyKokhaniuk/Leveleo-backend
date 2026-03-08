using LeveLEO.Features.Shipping.DTO;
using LeveLEO.Infrastructure.Delivery.DTO;

namespace LeveLEO.Features.Shipping.Services;

public interface IDeliveryService
{
    // Створення доставки для замовлення
    Task<DeliveryResponseDto> CreateDeliveryAsync(Guid orderId);

    // Отримання інформації про доставку
    Task<DeliveryResponseDto> GetDeliveryByIdAsync(Guid deliveryId);

    Task<DeliveryResponseDto> GetDeliveryByOrderIdAsync(Guid orderId);

    Task<DeliveryResponseDto> GetDeliveryByOrderNumberAsync(string orderNumber);

    Task<DeliveryResponseDto> GetDeliveryByTrackingNumberAsync(string trackingNumber);

    // Оновлення статусу доставки (з НП API)
    Task<DeliveryResponseDto> UpdateDeliveryStatusAsync(Guid deliveryId);

    // Скасування доставки
    Task<bool> CancelDeliveryAsync(Guid deliveryId);

    // Отримання історії трекінгу
    Task<List<TrackingEventDto>> GetTrackingHistoryAsync(Guid deliveryId);
}