namespace LeveLEO.Features.Newsletter.DTO;

/// <summary>
/// Інформація про активного підписника (з даними акаунту, якщо є)
/// </summary>
public class ActiveSubscriberDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset SubscribedAt { get; set; }
    public string? Source { get; set; }

    /// <summary>
    /// true — якщо цей email зареєстрований як акаунт на сайті
    /// </summary>
    public bool HasAccount { get; set; }

    /// <summary>
    /// Повне ім'я користувача (якщо є акаунт і вказане ім'я)
    /// </summary>
    public string? FullName { get; set; }
}