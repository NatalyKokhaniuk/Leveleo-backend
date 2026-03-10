namespace LeveLEO.Infrastructure.Events;

/// <summary>
/// Маркерний інтерфейс для всіх подій у системі
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Дата та час виникнення події
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
