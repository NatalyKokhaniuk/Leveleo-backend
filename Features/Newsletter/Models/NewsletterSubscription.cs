namespace LeveLEO.Features.Newsletter.Models;

/// <summary>
/// Підписка на розсилку новин
/// </summary>
public class NewsletterSubscription
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Email підписника
    /// </summary>
    public required string Email { get; set; }
    
    /// <summary>
    /// Токен для відписки (унікальний)
    /// </summary>
    public required string UnsubscribeToken { get; set; }
    
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
    /// IP адреса при підписці (для захисту від спаму)
    /// </summary>
    public string? SubscriberIpAddress { get; set; }
}
