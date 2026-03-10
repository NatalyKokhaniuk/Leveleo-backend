using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LeveLEO.Infrastructure.Events;

/// <summary>
/// In-memory реалізація шини подій
/// </summary>
public class InMemoryEventBus(IServiceProvider serviceProvider, ILogger<InMemoryEventBus> logger) : IEventBus
{
    private readonly Dictionary<Type, List<Type>> _handlers = new();
    private readonly object _lock = new();

    public void Subscribe<TEvent, THandler>()
        where TEvent : IEvent
        where THandler : IEventHandler<TEvent>
    {
        lock (_lock)
        {
            var eventType = typeof(TEvent);
            var handlerType = typeof(THandler);

            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = [];
            }

            if (!_handlers[eventType].Contains(handlerType))
            {
                _handlers[eventType].Add(handlerType);
                logger.LogInformation("Subscribed {HandlerType} to {EventType}", handlerType.Name, eventType.Name);
            }
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        List<Type> handlerTypes;

        lock (_lock)
        {
            if (!_handlers.ContainsKey(eventType))
            {
                logger.LogDebug("No handlers for event {EventType}", eventType.Name);
                return;
            }

            handlerTypes = [.. _handlers[eventType]];
        }

        logger.LogInformation("Publishing event {EventType} to {HandlerCount} handlers", eventType.Name, handlerTypes.Count);

        // Виконуємо обробники паралельно
        var tasks = handlerTypes.Select(async handlerType =>
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService(handlerType) as IEventHandler<TEvent>;
                if (handler != null)
                {
                    await handler.HandleAsync(@event);
                    logger.LogDebug("Handler {HandlerType} processed event {EventType}", handlerType.Name, eventType.Name);
                }
            }
            catch (Exception ex)
            {
                // Помилка в одному обробнику не повинна зупиняти інші
                logger.LogError(ex, "Error in handler {HandlerType} for event {EventType}", handlerType.Name, eventType.Name);
            }
        });

        await Task.WhenAll(tasks);
    }
}
