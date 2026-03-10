namespace LeveLEO.Infrastructure.Events.DomainEvents;

/// <summary>
/// Подія: новий відгук створено (потребує модерації)
/// </summary>
public class ReviewCreatedEvent : IEvent
{
    public Guid ReviewId { get; init; }
    public Guid OrderItemId { get; init; }
    public Guid ProductId { get; init; }
    public string UserId { get; init; } = null!;
    public string UserEmail { get; init; } = null!;
    public int Rating { get; init; }
    public string? ReviewText { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Подія: відгук схвалено модератором
/// </summary>
public class ReviewApprovedEvent : IEvent
{
    public Guid ReviewId { get; init; }
    public Guid ProductId { get; init; }
    public string UserId { get; init; } = null!;
    public string UserEmail { get; init; } = null!;
    public int Rating { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Подія: відгук відхилено модератором
/// </summary>
public class ReviewRejectedEvent : IEvent
{
    public Guid ReviewId { get; init; }
    public string UserId { get; init; } = null!;
    public string UserEmail { get; init; } = null!;
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
