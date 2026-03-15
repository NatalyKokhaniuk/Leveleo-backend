using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Newsletter.DTO;

/// <summary>
/// DTO для підписки на розсилку
/// </summary>
public class SubscribeNewsletterDto
{
    [Required(ErrorMessage = "Email обов'язковий")]
    [EmailAddress(ErrorMessage = "Невірний формат email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Джерело підписки (homepage, footer, popup)
    /// </summary>
    public string? Source { get; set; }
}

/// <summary>
/// DTO для відписки
/// </summary>
public class UnsubscribeNewsletterDto
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string UnsubscribeToken { get; set; } = string.Empty;
}

/// <summary>
/// Відповідь про статус підписки
/// </summary>
public class NewsletterSubscriptionResponseDto
{
    public bool IsSubscribed { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO для розсилки про новий продукт
/// </summary>
public class NewProductAnnouncementDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? MainImageUrl { get; set; }
    public string? ShortDescription { get; set; }
}

/// <summary>
/// DTO для розсилки про нову акцію
/// </summary>
public class NewPromotionAnnouncementDto
{
    public Guid PromotionId { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageKey { get; set; }
    
    /// <summary>
    /// Значення знижки (відсоток або фіксована сума)
    /// </summary>
    public decimal DiscountValue { get; set; }
    
    /// <summary>
    /// Тип знижки (Percentage або FixedAmount)
    /// </summary>
    public string DiscountType { get; set; } = string.Empty;
    
    /// <summary>
    /// Дата закінчення акції
    /// </summary>
    public DateTimeOffset EndDate { get; set; }
    
    /// <summary>
    /// Код купона (якщо потрібен)
    /// </summary>
    public string? CouponCode { get; set; }
}
