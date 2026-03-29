using LeveLEO.Data;
using LeveLEO.Features.AdminTasks.Models;
using LeveLEO.Features.AdminTasks.Services;
using LeveLEO.Features.ContactForms.DTO;
using LeveLEO.Features.ContactForms.Models;
using LeveLEO.Infrastructure.Events;
using LeveLEO.Infrastructure.Events.DomainEvents;
using System.Text.Json;

namespace LeveLEO.Features.ContactForms.Services;

public interface IContactFormService
{
    Task<ContactFormResponseDto> SubmitAsync(CreateContactFormDto dto);
}

public class ContactFormService(
    AppDbContext db,
    IAdminTaskService taskService,
    IEventBus eventBus,
    ILogger<ContactFormService> logger) : IContactFormService
{
    public async Task<ContactFormResponseDto> SubmitAsync(CreateContactFormDto dto)
    {
        var form = new ContactForm
        {
            Subject = dto.Subject.Trim(),
            Message = dto.Message.Trim(),
            Category = dto.Category,
            Email = dto.Email.ToLowerInvariant().Trim(),
            Phone = dto.Phone?.Trim()
        };

        db.ContactForms.Add(form);
        await db.SaveChangesAsync();

        logger.LogInformation("Contact form submitted: {Id} from {Email}, category: {Category}",
            form.Id, form.Email, form.Category);

        // Створити таску для адміна/модератора
        var metadata = JsonSerializer.Serialize(new
        {
            ContactFormId = form.Id,
            form.Email,
            form.Phone,
            Category = form.Category.ToString()
        });

        var task = new AdminTask
        {
            Title = $"Звернення: {form.Subject}",
            Description = $"Категорія: {GetCategoryDisplay(form.Category)}\n" +
                          $"Email: {form.Email}" +
                          (string.IsNullOrEmpty(form.Phone) ? "" : $"\nТелефон: {form.Phone}") +
                          $"\n\nТекст звернення:\n{form.Message}",
            Type = AdminTaskType.HandleContactForm,
            Priority = AdminTaskPriority.Normal,
            RelatedEntityId = form.Id,
            RelatedEntityType = "ContactForm",
            Metadata = metadata,
            CreatedBy = "System",
            RequesterEmail = form.Email
        };

        await taskService.CreateTaskAsync(task);

        // Надіслати підтвердження заявнику
        await eventBus.PublishAsync(new ContactFormReceivedEvent
        {
            RequesterEmail = form.Email,
            Subject = form.Subject,
            Message = form.Message,
            CategoryDisplay = GetCategoryDisplay(form.Category)
        });

        return new ContactFormResponseDto
        {
            Id = form.Id,
            Subject = form.Subject,
            Message = form.Message,
            Category = form.Category,
            CategoryDisplay = GetCategoryDisplay(form.Category),
            Email = form.Email,
            Phone = form.Phone,
            CreatedAt = form.CreatedAt
        };
    }

    private static string GetCategoryDisplay(ContactFormCategory category) => category switch
    {
        ContactFormCategory.DeliveryQuestion => "Питання по доставці",
        ContactFormCategory.OrderQuestion => "Питання по замовленню",
        ContactFormCategory.ReturnOrExchange => "Повернення / обмін",
        ContactFormCategory.ProductQuestion => "Питання по товару",
        ContactFormCategory.WebsiteQuestion => "Питання по сайту",
        ContactFormCategory.PaymentQuestion => "Оплата / рахунок",
        ContactFormCategory.Other => "Інше",
        _ => category.ToString()
    };
}
