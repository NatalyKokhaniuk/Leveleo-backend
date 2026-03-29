using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.AdminTasks.Models;

/// <summary>
/// Завдання для адміністраторів/модераторів
/// </summary>
public class AdminTask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;

    [Required]
    public AdminTaskType Type { get; set; }

    [Required]
    public AdminTaskPriority Priority { get; set; }

    [Required]
    public AdminTaskStatus Status { get; set; } = AdminTaskStatus.Pending;

    /// <summary>
    /// ID пов'язаної сутності (ReviewId, OrderId, etc.)
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// Тип пов'язаної сутності для навігації
    /// </summary>
    [MaxLength(50)]
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Додаткові дані у JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Хто призначив завдання (система або UserId)
    /// </summary>
    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Хто взяв завдання в роботу
    /// </summary>
    [MaxLength(450)]
    public string? AssignedTo { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Коментар адміна при виконанні
    /// </summary>
    public string? CompletionNote { get; set; }

    /// <summary>
    /// Email заявника (для форм зворотного зв'язку — щоб надіслати відповідь)
    /// </summary>
    [MaxLength(255)]
    public string? RequesterEmail { get; set; }
}

public enum AdminTaskType
{
    /// <summary>
    /// Модерувати відгук
    /// </summary>
    ModerateReview = 1,

    /// <summary>
    /// Відправити оплачене замовлення
    /// </summary>
    ShipOrder = 2,

    /// <summary>
    /// Повернути кошти за замовленням
    /// </summary>
    RefundOrder = 3,

    /// <summary>
    /// Перевірити розбіжність оплати
    /// </summary>
    InvestigatePayment = 4,

    /// <summary>
    /// Поповнити товар
    /// </summary>
    RestockProduct = 5,

    /// <summary>
    /// Обробити форму зворотного зв'язку
    /// </summary>
    HandleContactForm = 6,

    /// <summary>
    /// Інше
    /// </summary>
    Other = 99
}

public enum AdminTaskPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

public enum AdminTaskStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}