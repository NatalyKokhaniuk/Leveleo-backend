using LeveLEO.Features.Orders.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.Shipping.Models;

public class Delivery : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid AddressId { get; set; }

    public string TrackingNumber { get; set; } = null!;
    public string? NovaPoshtaDocumentRef { get; set; }

    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;

    public DateTimeOffset EstimatedDeliveryDate { get; set; }
    public DateTimeOffset? ActualDeliveryDate { get; set; }

    public decimal? DeliveryCost { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Order Order { get; set; } = null!;
    public Address Address { get; set; } = null!;
}

public enum DeliveryStatus
{
    Pending,

    Shipped,
    InTransit,
    OutForDelivery,
    Delivered,
    Canceled
}