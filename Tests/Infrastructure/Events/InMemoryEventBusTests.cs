using LeveLEO.Infrastructure.Events;
using LeveLEO.Infrastructure.Events.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LeveLEO.Tests.Infrastructure.Events;

public class InMemoryEventBusTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<ILogger<InMemoryEventBus>> _loggerMock;

    public InMemoryEventBusTests()
    {
        _loggerMock = new Mock<ILogger<InMemoryEventBus>>();
        
        var services = new ServiceCollection();
        services.AddScoped<TestEventHandler>();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void Subscribe_ShouldRegisterHandler()
    {
        // Arrange
        var eventBus = new InMemoryEventBus(_serviceProvider, _loggerMock.Object);

        // Act
        eventBus.Subscribe<TestEvent, TestEventHandler>();

        // Assert
        // Якщо не викинуло винятку - успішно підписано
        Assert.True(true);
    }

    [Fact]
    public async Task PublishAsync_ShouldCallHandler()
    {
        // Arrange
        var eventBus = new InMemoryEventBus(_serviceProvider, _loggerMock.Object);
        eventBus.Subscribe<TestEvent, TestEventHandler>();

        var testEvent = new TestEvent
        {
            Message = "Test message"
        };

        // Act
        await eventBus.PublishAsync(testEvent);

        // Assert
        // Handler було викликано (перевіримо через статичний лічильник)
        Assert.Equal(1, TestEventHandler.CallCount);
    }

    [Fact]
    public async Task PublishAsync_WithoutHandlers_ShouldNotThrow()
    {
        // Arrange
        var eventBus = new InMemoryEventBus(_serviceProvider, _loggerMock.Object);
        var testEvent = new TestEvent { Message = "Test" };

        // Act & Assert
        await eventBus.PublishAsync(testEvent);
        // Не повинно викинути виняток
    }
}

// Test event and handler
public class TestEvent : IEvent
{
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public class TestEventHandler : IEventHandler<TestEvent>
{
    public static int CallCount { get; private set; }

    public Task HandleAsync(TestEvent @event)
    {
        CallCount++;
        return Task.CompletedTask;
    }
}
