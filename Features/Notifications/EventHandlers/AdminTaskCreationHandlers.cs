using LeveLEO.Features.AdminTasks.Models;
using LeveLEO.Features.AdminTasks.Services;
using LeveLEO.Infrastructure.Events;
using LeveLEO.Infrastructure.Events.DomainEvents;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LeveLEO.Features.Notifications.EventHandlers;

/// <summary>
/// Створює admin task для модерації нового відгуку
/// </summary>
public class ReviewCreatedTaskHandler(
    IAdminTaskService taskService,
    ILogger<ReviewCreatedTaskHandler> logger) : IEventHandler<ReviewCreatedEvent>
{
    public async Task HandleAsync(ReviewCreatedEvent @event)
    {
        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                @event.ProductId,
                @event.UserId,
                @event.Rating,
                ReviewText = @event.ReviewText?.Substring(0, Math.Min(100, @event.ReviewText?.Length ?? 0))
            });

            var task = new AdminTask
            {
                Title = "Модерувати новий відгук",
                Description = $"Користувач залишив відгук з оцінкою {@event.Rating}/5. Потребує модерації.",
                Type = AdminTaskType.ModerateReview,
                Priority = AdminTaskPriority.Normal,
                RelatedEntityId = @event.ReviewId,
                RelatedEntityType = "Review",
                Metadata = metadata,
                CreatedBy = "System"
            };

            await taskService.CreateTaskAsync(task);
            logger.LogInformation("Admin task created for review moderation: {ReviewId}", @event.ReviewId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create admin task for review {ReviewId}", @event.ReviewId);
        }
    }
}

/// <summary>
/// Створює admin task для відправки оплаченого замовлення
/// </summary>
public class OrderPaidTaskHandler(
    IAdminTaskService taskService,
    ILogger<OrderPaidTaskHandler> logger) : IEventHandler<OrderPaidEvent>
{
    public async Task HandleAsync(OrderPaidEvent @event)
    {
        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                @event.OrderNumber,
                @event.AmountPaid,
                @event.UserId
            });

            var task = new AdminTask
            {
                Title = $"Відправити замовлення #{@event.OrderNumber}",
                Description = $"Замовлення на суму {@event.AmountPaid:C} оплачено. Потрібно підготувати та відправити.",
                Type = AdminTaskType.ShipOrder,
                Priority = AdminTaskPriority.High,
                RelatedEntityId = @event.OrderId,
                RelatedEntityType = "Order",
                Metadata = metadata,
                CreatedBy = "System"
            };

            await taskService.CreateTaskAsync(task);
            logger.LogInformation("Admin task created for shipping order: {OrderNumber}", @event.OrderNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create admin task for order {OrderId}", @event.OrderId);
        }
    }
}

/// <summary>
/// Створює критичний admin task при розбіжності оплати та статусу замовлення
/// </summary>
public class PaymentMismatchTaskHandler(
    IAdminTaskService taskService,
    ILogger<PaymentMismatchTaskHandler> logger) : IEventHandler<PaymentOrderMismatchEvent>
{
    public async Task HandleAsync(PaymentOrderMismatchEvent @event)
    {
        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                @event.OrderNumber,
                @event.PaymentId,
                @event.Reason,
                @event.UserId
            });

            var task = new AdminTask
            {
                Title = $"КРИТИЧНО: Розбіжність оплати замовлення #{@event.OrderNumber}",
                Description = $"Причина: {@event.Reason}. Потрібна негайна увага! Можливо потрібен рефанд або з'ясування з платіжною системою.",
                Type = AdminTaskType.InvestigatePayment,
                Priority = AdminTaskPriority.Critical,
                RelatedEntityId = @event.OrderId,
                RelatedEntityType = "Order",
                Metadata = metadata,
                CreatedBy = "System"
            };

            await taskService.CreateTaskAsync(task);
            logger.LogCritical("Critical admin task created for payment mismatch: Order {OrderNumber}, Payment {PaymentId}", 
                @event.OrderNumber, @event.PaymentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create critical admin task for payment mismatch {OrderId}", @event.OrderId);
        }
    }
}
