using LeveLEO.Infrastructure.Email;
using LeveLEO.Infrastructure.Events;
using LeveLEO.Infrastructure.Events.DomainEvents;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Identity.UI.Services;

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
            var body = $@"
                <h2>Дякуємо за замовлення!</h2>
                <p>Ваше замовлення <strong>#{@event.OrderNumber}</strong> успішно створено.</p>
                <p>Сума до оплати: <strong>{@event.TotalPayable:C}</strong></p>
                <p>Будь ласка, завершіть оплату для обробки замовлення.</p>
                <p>З повагою,<br/>Команда LeveLEO</p>
            ";

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
    ILogger<OrderPaidEmailHandler> logger) : IEventHandler<OrderPaidEvent>
{
    public async Task HandleAsync(OrderPaidEvent @event)
    {
        try
        {
            var subject = $"Замовлення #{@event.OrderNumber} оплачено";
            var body = $@"
                <h2>Оплату отримано!</h2>
                <p>Ваше замовлення <strong>#{@event.OrderNumber}</strong> успішно оплачено.</p>
                <p>Сума: <strong>{@event.AmountPaid:C}</strong></p>
                <p>Ми розпочинаємо підготовку вашого замовлення до відправки.</p>
                <p>З повагою,<br/>Команда LeveLEO</p>
            ";

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
    ILogger<OrderShippedEmailHandler> logger) : IEventHandler<OrderShippedEvent>
{
    public async Task HandleAsync(OrderShippedEvent @event)
    {
        try
        {
            var subject = $"Замовлення #{@event.OrderNumber} відправлено";
            var trackingInfo = !string.IsNullOrEmpty(@event.TrackingNumber)
                ? $"<p>Номер відстеження: <strong>{@event.TrackingNumber}</strong></p>"
                : "";

            var body = $@"
                <h2>Ваше замовлення в дорозі!</h2>
                <p>Замовлення <strong>#{@event.OrderNumber}</strong> відправлено.</p>
                {trackingInfo}
                <p>Ви можете відстежити статус доставки в особистому кабінеті.</p>
                <p>З повагою,<br/>Команда LeveLEO</p>
            ";

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
    ILogger<OrderCompletedEmailHandler> logger) : IEventHandler<OrderCompletedEvent>
{
    public async Task HandleAsync(OrderCompletedEvent @event)
    {
        try
        {
            var subject = $"Замовлення #{@event.OrderNumber} доставлено";
            var body = $@"
                <h2>Замовлення доставлено!</h2>
                <p>Ваше замовлення <strong>#{@event.OrderNumber}</strong> успішно доставлено.</p>
                <p>Дякуємо, що обрали LeveLEO!</p>
                <p>Будемо вдячні, якщо ви залишите відгук про товари в особистому кабінеті.</p>
                <p>З повагою,<br/>Команда LeveLEO</p>
            ";

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
    ILogger<ReviewApprovedEmailHandler> logger) : IEventHandler<ReviewApprovedEvent>
{
    public async Task HandleAsync(ReviewApprovedEvent @event)
    {
        try
        {
            var subject = "Ваш відгук опубліковано";
            var body = $@"
                <h2>Відгук схвалено!</h2>
                <p>Ваш відгук успішно пройшов модерацію та опублікований на сайті.</p>
                <p>Дякуємо за вашу думку!</p>
                <p>З повагою,<br/>Команда LeveLEO</p>
            ";

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
    ILogger<ReviewRejectedEmailHandler> logger) : IEventHandler<ReviewRejectedEvent>
{
    public async Task HandleAsync(ReviewRejectedEvent @event)
    {
        try
        {
            var subject = "Про ваш відгук";
            var body = $@"
                <h2>Відгук не пройшов модерацію</h2>
                <p>На жаль, ваш відгук не відповідає правилам публікації на нашому сайті.</p>
                <p>Будь ласка, переконайтесь, що відгук стосується товару та не містить неприйнятного контенту.</p>
                <p>З повагою,<br/>Команда LeveLEO</p>
            ";

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