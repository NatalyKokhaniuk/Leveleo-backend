using LeveLEO.Features.Orders.Models;
using LeveLEO.Features.Shipping.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.Shipping.DTO;

public class DeliveryResponseDto : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string TrackingNumber { get; set; } = null!;
    public DeliveryStatus Status { get; set; }
    public DateTimeOffset EstimatedDeliveryDate { get; set; }
    public DateTimeOffset? ActualDeliveryDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public AddressResponseDto Address { get; set; } = null!;

    public string? NovaPoshtaDocumentRef { get; set; } // Ref накладної в НП
    public decimal? DeliveryCost { get; set; } // Вартість доставки
}