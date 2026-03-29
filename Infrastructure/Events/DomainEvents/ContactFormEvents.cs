using LeveLEO.Infrastructure.Events;

namespace LeveLEO.Infrastructure.Events.DomainEvents;

/// <summary>
/// Подія: форма зворотного зв'язку отримана (підтвердження заявнику)
/// </summary>
public class ContactFormReceivedEvent : IEvent
{
    public string RequesterEmail { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string CategoryDisplay { get; init; } = string.Empty;

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Подія: адмін опрацював форму зворотного зв'язку і залишив коментар
/// </summary>
public class ContactFormResolvedEvent : IEvent
{
    public string RequesterEmail { get; init; } = string.Empty;
    public string TaskTitle { get; init; } = string.Empty;
    public string AdminNote { get; init; } = string.Empty;
    public DateTimeOffset ResolvedAt { get; init; }

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
