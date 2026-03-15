using LeveLEO.Infrastructure.Events;

namespace LeveLEO.Infrastructure.Events.DomainEvents;

/// <summary>
/// Подія створення нового продукту (для розсилки)
/// </summary>
public class ProductCreatedEvent : IEvent
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductSlug { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string? MainImageUrl { get; init; }
    public string? ShortDescription { get; init; }
    
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Подія створення нової акції (для розсилки)
/// </summary>
public class PromotionCreatedEvent : IEvent
{
    public Guid PromotionId { get; init; }
    public string PromotionName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageKey { get; init; }
    
    /// <summary>
    /// Відсоток або фіксована знижка (для відображення в email)
    /// </summary>
    public decimal DiscountValue { get; init; }
    
    /// <summary>
    /// Тип знижки (Percentage або FixedAmount)
    /// </summary>
    public string DiscountType { get; init; } = string.Empty;
    
    /// <summary>
    /// Дата початку акції
    /// </summary>
    public DateTimeOffset StartDate { get; init; }
    
    /// <summary>
    /// Дата закінчення акції
    /// </summary>
    public DateTimeOffset EndDate { get; init; }
    
    /// <summary>
    /// Код купона (якщо є)
    /// </summary>
    public string? CouponCode { get; init; }
    
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
