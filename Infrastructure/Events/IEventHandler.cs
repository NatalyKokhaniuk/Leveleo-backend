namespace LeveLEO.Infrastructure.Events;

/// <summary>
/// Обробник події
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    /// <summary>
    /// Обробити подію
    /// </summary>
    Task HandleAsync(TEvent @event);
}
