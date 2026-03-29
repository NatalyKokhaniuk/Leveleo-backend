using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.ContactForms.Models;

/// <summary>
/// Форма зворотного зв'язку від відвідувача сайту
/// </summary>
public class ContactForm
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Тема звернення
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = null!;

    /// <summary>
    /// Текст звернення
    /// </summary>
    [Required]
    public string Message { get; set; } = null!;

    /// <summary>
    /// Категорія звернення
    /// </summary>
    [Required]
    public ContactFormCategory Category { get; set; }

    /// <summary>
    /// Email заявника (обов'язковий)
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = null!;

    /// <summary>
    /// Телефон заявника (необов'язковий)
    /// </summary>
    [MaxLength(30)]
    public string? Phone { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum ContactFormCategory
{
    /// <summary>
    /// Питання по доставці
    /// </summary>
    DeliveryQuestion = 1,

    /// <summary>
    /// Питання по замовленню
    /// </summary>
    OrderQuestion = 2,

    /// <summary>
    /// Повернення / обмін
    /// </summary>
    ReturnOrExchange = 3,

    /// <summary>
    /// Питання по товару
    /// </summary>
    ProductQuestion = 4,

    /// <summary>
    /// Питання по сайту
    /// </summary>
    WebsiteQuestion = 5,

    /// <summary>
    /// Оплата / рахунок
    /// </summary>
    PaymentQuestion = 6,

    /// <summary>
    /// Інше
    /// </summary>
    Other = 99
}
