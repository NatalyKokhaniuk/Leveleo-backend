using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Newsletter.Models;

/// <summary>
/// Підписник на розсилку новин
/// </summary>
public class NewsletterSubscriber
{
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Токен для відписки (безпечний unsubscribe)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string UnsubscribeToken { get; set; } = string.Empty;

    /// <summary>
    /// Чи активна підписка
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Дата підписки
    /// </summary>
    public DateTimeOffset SubscribedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Дата відписки (якщо відписався)
    /// </summary>
    public DateTimeOffset? UnsubscribedAt { get; set; }

    /// <summary>
    /// IP адреса при підписці (для безпеки)
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Джерело підписки (homepage, footer, popup, тощо)
    /// </summary>
    [MaxLength(50)]
    public string? Source { get; set; }
}
