using LeveLEO.Infrastructure.Email;
using LeveLEO.Infrastructure.Events;
using LeveLEO.Infrastructure.Events.DomainEvents;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace LeveLEO.Features.Notifications.EventHandlers;

/// <summary>
/// Надсилає заявнику підтвердження отримання форми зворотного зв'язку
/// </summary>
public class ContactFormReceivedEmailHandler(
    IEmailSender emailSender,
    IEmailTemplateService templateService,
    ILogger<ContactFormReceivedEmailHandler> logger) : IEventHandler<ContactFormReceivedEvent>
{
    public async Task HandleAsync(ContactFormReceivedEvent @event)
    {
        try
        {
            var subject = "Ваше звернення отримано — LeveLEO";

            var replacements = new Dictionary<string, string>
            {
                { "{{REQUESTER_EMAIL}}", @event.RequesterEmail },
                { "{{SUBJECT}}", @event.Subject },
                { "{{CATEGORY}}", @event.CategoryDisplay },
                { "{{MESSAGE}}", @event.Message }
            };

            var body = await templateService.GetTemplateAsync("ContactFormReceived", replacements);

            await emailSender.SendEmailAsync(@event.RequesterEmail, subject, body);

            logger.LogInformation("Contact form confirmation sent to {Email}", @event.RequesterEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send contact form confirmation to {Email}", @event.RequesterEmail);
        }
    }
}

/// <summary>
/// Надсилає заявнику відповідь адміна по його зверненню
/// </summary>
public class ContactFormResolvedEmailHandler(
    IEmailSender emailSender,
    IEmailTemplateService templateService,
    ILogger<ContactFormResolvedEmailHandler> logger) : IEventHandler<ContactFormResolvedEvent>
{
    public async Task HandleAsync(ContactFormResolvedEvent @event)
    {
        try
        {
            var subject = "Відповідь на ваше звернення — LeveLEO";

            var replacements = new Dictionary<string, string>
            {
                { "{{TASK_TITLE}}", @event.TaskTitle },
                { "{{ADMIN_NOTE}}", @event.AdminNote },
                { "{{RESOLVED_AT}}", @event.ResolvedAt.ToLocalTime().ToString("dd.MM.yyyy о HH:mm") }
            };

            var body = await templateService.GetTemplateAsync("ContactFormResolved", replacements);

            await emailSender.SendEmailAsync(@event.RequesterEmail, subject, body);

            logger.LogInformation("Contact form resolved email sent to {Email}", @event.RequesterEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send contact form resolved email to {Email}", @event.RequesterEmail);
        }
    }
}
