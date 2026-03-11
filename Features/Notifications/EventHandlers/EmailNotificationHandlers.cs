using LeveLEO.Infrastructure.Email;
using LeveLEO.Infrastructure.Events;
using LeveLEO.Infrastructure.Events.DomainEvents;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace LeveLEO.Features.Notifications.EventHandlers;

/// <summary>
/// Відправляє email при створенні замовлення
/// </summary>
public class OrderCreatedEmailHandler(
    IEmailSender emailSender,
    IEmailTemplateService templateService,
    ILogger<OrderCreatedEmailHandler> logger) : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        try
        {
            var subject = $"Замовлення #{@event.OrderNumber} створено";

            var replacements = new Dictionary<string, string>
            {
                { "{{ORDER_NUMBER}}", @event.OrderNumber },
                { "{{TOTAL_PAYABLE}}", $"{@event.TotalPayable:C}" },
                { "{{ORDER_LINK}}", $"https://leveleo.com/orders/{@event.OrderId}" }
            };

            var body = await templateService.GetTemplateAsync("OrderCreated", replacements);

            await emailSender.SendEmailAsync(@event.UserEmail, subject, body);
            logger.LogInformation("Order created email sent to {Email} for order {OrderNumber}",
                @event.UserEmail, @event.OrderNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send order created email for order {OrderId}", @event.OrderId);
        }
    }
}

/// <summary>
/// Відправляє email при оплаті замовлення
/// </summary>
public class OrderPaidEmailHandler(
    IEmailSender emailSender,
    IEmailTemplateService templateService,
    ILogger<OrderPaidEmailHandler> logger) : IEventHandler<OrderPaidEvent>
{
    public async Task HandleAsync(OrderPaidEvent @event)
    {
        try
        {
            var subject = $"Замовлення #{@event.OrderNumber} оплачено";

            var replacements = new Dictionary<string, string>
            {
                { "{{ORDER_NUMBER}}", @event.OrderNumber },
                { "{{AMOUNT_PAID}}", $"{@event.AmountPaid:C}" },
                { "{{ORDER_LINK}}", $"https://leveleo.com/orders/{@event.OrderId}" }
            };

            var body = await templateService.GetTemplateAsync("OrderPaid", replacements);

            await emailSender.SendEmailAsync(@event.UserEmail, subject, body);
            logger.LogInformation("Order paid email sent to {Email} for order {OrderNumber}",
                @event.UserEmail, @event.OrderNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send order paid email for order {OrderId}", @event.OrderId);
        }
    }
}

/// <summary>
/// Відправляє email при відправці замовлення
/// </summary>
public class OrderShippedEmailHandler(
    IEmailSender emailSender,
    IEmailTemplateService templateService,
    ILogger<OrderShippedEmailHandler> logger) : IEventHandler<OrderShippedEvent>
{
    public async Task HandleAsync(OrderShippedEvent @event)
    {
        try
        {
            var subject = $"Замовлення #{@event.OrderNumber} відправлено";

            var trackingInfo = !string.IsNullOrEmpty(@event.TrackingNumber)
                ? $"<p style='margin: 8px 0; font-size: 14px;'><strong>Номер відстеження (ТТН):</strong> <span style='color: #f59e0b; font-weight: bold;'>{@event.TrackingNumber}</span></p>"
                : "<p style='margin: 8px 0; font-size: 14px; color: #888;'>Номер ТТН буде доступний незабаром</p>";

            var replacements = new Dictionary<string, string>
            {
                { "{{ORDER_NUMBER}}", @event.OrderNumber },
                { "{{TRACKING_INFO}}", trackingInfo },
                { "{{ORDER_LINK}}", $"https://leveleo.com/orders/{@event.OrderId}" }
            };

            var body = await templateService.GetTemplateAsync("OrderShipped", replacements);

            await emailSender.SendEmailAsync(@event.UserEmail, subject, body);
            logger.LogInformation("Order shipped email sent to {Email} for order {OrderNumber}",
                @event.UserEmail, @event.OrderNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send order shipped email for order {OrderId}", @event.OrderId);
        }
    }
}

/// <summary>
/// Відправляє email при доставці замовлення
/// </summary>
public class OrderCompletedEmailHandler(
    IEmailSender emailSender,
    IEmailTemplateService templateService,
    ILogger<OrderCompletedEmailHandler> logger) : IEventHandler<OrderCompletedEvent>
{
    public async Task HandleAsync(OrderCompletedEvent @event)
    {
        try
        {
            var subject = $"Замовлення #{@event.OrderNumber} доставлено";

            var replacements = new Dictionary<string, string>
            {
                { "{{ORDER_NUMBER}}", @event.OrderNumber },
                { "{{ORDER_LINK}}", $"https://leveleo.com/orders/{@event.OrderId}" }
            };

            var body = await templateService.GetTemplateAsync("OrderCompleted", replacements);

            await emailSender.SendEmailAsync(@event.UserEmail, subject, body);
            logger.LogInformation("Order completed email sent to {Email} for order {OrderNumber}",
                @event.UserEmail, @event.OrderNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send order completed email for order {OrderId}", @event.OrderId);
        }
    }
}

/// <summary>
/// Відправляє email при схваленні відгуку
/// </summary>
public class ReviewApprovedEmailHandler(
    IEmailSender emailSender,
    IEmailTemplateService templateService,
    ILogger<ReviewApprovedEmailHandler> logger) : IEventHandler<ReviewApprovedEvent>
{
    public async Task HandleAsync(ReviewApprovedEvent @event)
    {
        try
        {
            var subject = "Ваш відгук опубліковано";

            var replacements = new Dictionary<string, string>
            {
                { "{{PRODUCT_LINK}}", $"https://leveleo.com/products/{@event.ProductId}" }
            };

            var body = await templateService.GetTemplateAsync("ReviewApproved", replacements);

            await emailSender.SendEmailAsync(@event.UserEmail, subject, body);
            logger.LogInformation("Review approved email sent to {Email} for review {ReviewId}",
                @event.UserEmail, @event.ReviewId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send review approved email for review {ReviewId}", @event.ReviewId);
        }
    }
}

/// <summary>
/// Відправляє email при відхиленні відгуку
/// </summary>
public class ReviewRejectedEmailHandler(
    IEmailSender emailSender,
    IEmailTemplateService templateService,
    ILogger<ReviewRejectedEmailHandler> logger) : IEventHandler<ReviewRejectedEvent>
{
    public async Task HandleAsync(ReviewRejectedEvent @event)
    {
        try
        {
            var subject = "Про ваш відгук";

            var replacements = new Dictionary<string, string>
            {
                { "{{ORDER_LINK}}", $"https://leveleo.com/my-reviews" }
            };

            var body = await templateService.GetTemplateAsync("ReviewRejected", replacements);

            await emailSender.SendEmailAsync(@event.UserEmail, subject, body);
            logger.LogInformation("Review rejected email sent to {Email} for review {ReviewId}",
                @event.UserEmail, @event.ReviewId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send review rejected email for review {ReviewId}", @event.ReviewId);
        }
    }
}