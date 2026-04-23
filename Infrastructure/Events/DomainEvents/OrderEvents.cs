using LeveLEO.Features.Orders.Models;

namespace LeveLEO.Infrastructure.Events.DomainEvents;

/// <summary>
/// Подія: замовлення створено та очікує оплати
/// </summary>
public class OrderCreatedEvent : IEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string UserEmail { get; init; } = null!;
    public OrderStatus Status { get; init; }
    public decimal TotalPayable { get; init; }
    public decimal TotalOriginalPrice { get; init; }
    public decimal TotalProductDiscount { get; init; }
    public decimal TotalCartDiscount { get; init; }
    public string DeliveryAddress { get; init; } = null!;
    public List<OrderCreatedItemSnapshot> Items { get; init; } = [];
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public class OrderCreatedItemSnapshot
{
    public string ProductName { get; init; } = null!;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountedUnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}

/// <summary>
/// Подія: статус замовлення змінився
/// </summary>
public class OrderStatusChangedEvent : IEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string UserEmail { get; init; } = null!;
    public OrderStatus OldStatus { get; init; }
    public OrderStatus NewStatus { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Подія: замовлення оплачено, потребує відправки
/// </summary>
public class OrderPaidEvent : IEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string UserEmail { get; init; } = null!;
    public decimal AmountPaid { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Подія: замовлення відправлено
/// </summary>
public class OrderShippedEvent : IEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string UserEmail { get; init; } = null!;
    public string? TrackingNumber { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Подія: замовлення доставлено
/// </summary>
public class OrderCompletedEvent : IEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string UserEmail { get; init; } = null!;
    public List<Guid> ProductIds { get; init; } = [];
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Подія: оплата не пройшла, потрібна увага адміна (рефанд або з'ясування)
/// </summary>
public class PaymentOrderMismatchEvent : IEvent
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string Reason { get; init; } = null!;
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
