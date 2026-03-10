using System.Threading.Tasks;

namespace LeveLEO.Infrastructure.Events;

/// <summary>
/// Шина подій для публікації та підписки на події в системі
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Опублікувати подію всім підписникам
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent;

    /// <summary>
    /// Підписатися на обробку події
    /// </summary>
    void Subscribe<TEvent, THandler>()
        where TEvent : IEvent
        where THandler : IEventHandler<TEvent>;
}
