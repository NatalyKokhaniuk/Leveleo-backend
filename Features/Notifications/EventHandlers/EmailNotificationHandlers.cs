using LeveLEO.Infrastructure.Email;
using LeveLEO.Infrastructure.Events;
using LeveLEO.Infrastructure.Events.DomainEvents;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

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
                { "{{ORDER_STATUS}}", MapOrderStatus(@event.Status) },
                { "{{DELIVERY_ADDRESS}}", @event.DeliveryAddress },
                { "{{ORDER_ITEMS}}", BuildOrderItemsHtml(@event.Items) },
                { "{{TOTAL_ORIGINAL}}", FormatCurrency(@event.TotalOriginalPrice) },
                { "{{TOTAL_PRODUCT_DISCOUNT}}", FormatCurrency(@event.TotalProductDiscount) },
                { "{{TOTAL_CART_DISCOUNT}}", FormatCurrency(@event.TotalCartDiscount) },
                { "{{TOTAL_PAYABLE}}", FormatCurrency(@event.TotalPayable) },
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

    private static string BuildOrderItemsHtml(IReadOnlyCollection<OrderCreatedItemSnapshot> items)
    {
        if (items.Count == 0)
            return "<p style='font-size: 14px; color: #555;'>Склад замовлення буде доступний у вашому кабінеті.</p>";

        var sb = new StringBuilder();
        sb.Append("<table style='width: 100%; border-collapse: collapse; margin-top: 10px;'>");
        sb.Append("<thead><tr>");
        sb.Append("<th style='text-align: left; font-size: 13px; color: #666; border-bottom: 1px solid #e5e7eb; padding: 8px 0;'>Товар</th>");
        sb.Append("<th style='text-align: center; font-size: 13px; color: #666; border-bottom: 1px solid #e5e7eb; padding: 8px 0;'>К-сть</th>");
        sb.Append("<th style='text-align: right; font-size: 13px; color: #666; border-bottom: 1px solid #e5e7eb; padding: 8px 0;'>Ціна</th>");
        sb.Append("<th style='text-align: right; font-size: 13px; color: #666; border-bottom: 1px solid #e5e7eb; padding: 8px 0;'>Сума</th>");
        sb.Append("</tr></thead><tbody>");

        foreach (var item in items)
        {
            sb.Append("<tr>");
            sb.Append($"<td style='padding: 10px 0; border-bottom: 1px solid #f1f5f9; font-size: 14px; color: #111827;'>{item.ProductName}</td>");
            sb.Append($"<td style='padding: 10px 0; border-bottom: 1px solid #f1f5f9; text-align: center; font-size: 14px; color: #111827;'>{item.Quantity}</td>");
            sb.Append($"<td style='padding: 10px 0; border-bottom: 1px solid #f1f5f9; text-align: right; font-size: 14px; color: #111827;'>{FormatCurrency(item.DiscountedUnitPrice)}</td>");
            sb.Append($"<td style='padding: 10px 0; border-bottom: 1px solid #f1f5f9; text-align: right; font-size: 14px; color: #111827;'>{FormatCurrency(item.LineTotal)}</td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string MapOrderStatus(Features.Orders.Models.OrderStatus status)
    {
        return status switch
        {
            Features.Orders.Models.OrderStatus.Pending => "Очікує оплати",
            Features.Orders.Models.OrderStatus.Processing => "В обробці",
            Features.Orders.Models.OrderStatus.Shipped => "Відправлено",
            Features.Orders.Models.OrderStatus.Completed => "Завершено",
            Features.Orders.Models.OrderStatus.Cancelled => "Скасовано",
            Features.Orders.Models.OrderStatus.PaymentFailed => "Помилка оплати",
            _ => status.ToString()
        };
    }

    private static string FormatCurrency(decimal amount)
    {
        var culture = CultureInfo.GetCultureInfo("uk-UA");
        return $"{amount.ToString("N2", culture)} грн";
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