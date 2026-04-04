using LeveLEO.Features.Newsletter.DTO;

namespace LeveLEO.Features.Newsletter.Services;

public interface INewsletterService
{
    /// <summary>
    /// Підписатися на розсилку
    /// </summary>
    Task<NewsletterSubscriptionResponseDto> SubscribeAsync(SubscribeNewsletterDto dto, string? ipAddress);

    /// <summary>
    /// Відписатися від розсилки
    /// </summary>
    Task<NewsletterSubscriptionResponseDto> UnsubscribeAsync(UnsubscribeNewsletterDto dto);

    /// <summary>
    /// Відписатися по токену (з email посилання)
    /// </summary>
    Task<NewsletterSubscriptionResponseDto> UnsubscribeByTokenAsync(string token);

    /// <summary>
    /// Перевірити чи email підписаний
    /// </summary>
    Task<bool> IsSubscribedAsync(string email);

    /// <summary>
    /// Розіслати повідомлення про новий продукт всім підписникам
    /// </summary>
    Task SendNewProductAnnouncementAsync(NewProductAnnouncementDto product);

    /// <summary>
    /// Розіслати повідомлення про нову акцію всім підписникам
    /// </summary>
    Task SendNewPromotionAnnouncementAsync(NewPromotionAnnouncementDto promotion);

    /// <summary>
    /// Отримати кількість активних підписників
    /// </summary>
    Task<int> GetActiveSubscribersCountAsync();

    /// <summary>
    /// Отримати активних підписників
    /// </summary>

    Task<IEnumerable<ActiveSubscriberDto>> GetActiveSubscribersAsync();
}