using LeveLEO.Data;
using LeveLEO.Features.Newsletter.DTO;
using LeveLEO.Features.Newsletter.Models;
using LeveLEO.Infrastructure.Email;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace LeveLEO.Features.Newsletter.Services;

public class NewsletterService : INewsletterService
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateService _templateService;
    private readonly ILogger<NewsletterService> _logger;

    public NewsletterService(
        AppDbContext db,
        IEmailSender emailSender,
        IEmailTemplateService templateService,
        ILogger<NewsletterService> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _templateService = templateService;
        _logger = logger;
    }

    public async Task<NewsletterSubscriptionResponseDto> SubscribeAsync(SubscribeNewsletterDto dto, string? ipAddress)
    {
        var email = dto.Email.ToLowerInvariant().Trim();

        // Перевірити чи вже підписаний
        var existing = await _db.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == email);

        if (existing != null)
        {
            if (existing.IsActive)
            {
                return new NewsletterSubscriptionResponseDto
                {
                    IsSubscribed = true,
                    Message = "Ви вже підписані на нашу розсилку!"
                };
            }
            else
            {
                // Поновити підписку
                existing.IsActive = true;
                existing.SubscribedAt = DateTimeOffset.UtcNow;
                existing.UnsubscribedAt = null;
                existing.UnsubscribeToken = GenerateUnsubscribeToken();
                await _db.SaveChangesAsync();

                await SendWelcomeEmailAsync(existing);

                return new NewsletterSubscriptionResponseDto
                {
                    IsSubscribed = true,
                    Message = "Підписку поновлено! Перевірте вашу пошту."
                };
            }
        }

        // Створити нового підписника
        var subscriber = new NewsletterSubscriber
        {
            Email = email,
            UnsubscribeToken = GenerateUnsubscribeToken(),
            IsActive = true,
            IpAddress = ipAddress,
            Source = dto.Source
        };

        _db.NewsletterSubscribers.Add(subscriber);
        await _db.SaveChangesAsync();

        // Відправити welcome email
        await SendWelcomeEmailAsync(subscriber);

        _logger.LogInformation("New newsletter subscriber: {Email}", email);

        return new NewsletterSubscriptionResponseDto
        {
            IsSubscribed = true,
            Message = "Дякуємо за підписку! Перевірте вашу пошту для підтвердження."
        };
    }

    public async Task<NewsletterSubscriptionResponseDto> UnsubscribeAsync(UnsubscribeNewsletterDto dto)
    {
        var email = dto.Email.ToLowerInvariant().Trim();

        var subscriber = await _db.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == email && s.UnsubscribeToken == dto.UnsubscribeToken);

        if (subscriber == null)
        {
            return new NewsletterSubscriptionResponseDto
            {
                IsSubscribed = false,
                Message = "Підписку не знайдено або невірний токен."
            };
        }

        subscriber.IsActive = false;
        subscriber.UnsubscribedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Newsletter unsubscribe: {Email}", email);

        return new NewsletterSubscriptionResponseDto
        {
            IsSubscribed = false,
            Message = "Ви успішно відписалися від розсилки. Сумуватимемо за вами! 😢"
        };
    }

    public async Task<NewsletterSubscriptionResponseDto> UnsubscribeByTokenAsync(string token)
    {
        var subscriber = await _db.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.UnsubscribeToken == token && s.IsActive);

        if (subscriber == null)
        {
            return new NewsletterSubscriptionResponseDto
            {
                IsSubscribed = false,
                Message = "Підписку не знайдено або вже відписано."
            };
        }

        subscriber.IsActive = false;
        subscriber.UnsubscribedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Newsletter unsubscribe by token: {Email}", subscriber.Email);

        return new NewsletterSubscriptionResponseDto
        {
            IsSubscribed = false,
            Message = "Ви успішно відписалися від розсилки."
        };
    }

    public async Task<bool> IsSubscribedAsync(string email)
    {
        var normalized = email.ToLowerInvariant().Trim();
        return await _db.NewsletterSubscribers
            .AnyAsync(s => s.Email == normalized && s.IsActive);
    }

    public async Task SendNewProductAnnouncementAsync(NewProductAnnouncementDto product)
    {
        var subscribers = await _db.NewsletterSubscribers
            .Where(s => s.IsActive)
            .ToListAsync();

        if (subscribers.Count == 0)
        {
            _logger.LogWarning("No active newsletter subscribers for new product announcement");
            return;
        }

        _logger.LogInformation("Sending new product announcement to {Count} subscribers", subscribers.Count);

        foreach (var subscriber in subscribers)
        {
            try
            {
                await SendNewProductEmailAsync(subscriber, product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send new product email to {Email}", subscriber.Email);
            }
        }
    }

    public async Task SendNewPromotionAnnouncementAsync(NewPromotionAnnouncementDto promotion)
    {
        var subscribers = await _db.NewsletterSubscribers
            .Where(s => s.IsActive)
            .ToListAsync();

        if (subscribers.Count == 0)
        {
            _logger.LogWarning("No active newsletter subscribers for new promotion announcement");
            return;
        }

        _logger.LogInformation("Sending new promotion announcement to {Count} subscribers", subscribers.Count);

        foreach (var subscriber in subscribers)
        {
            try
            {
                await SendNewPromotionEmailAsync(subscriber, promotion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send new promotion email to {Email}", subscriber.Email);
            }
        }
    }

    public async Task<int> GetActiveSubscribersCountAsync()
    {
        return await _db.NewsletterSubscribers.CountAsync(s => s.IsActive);
    }

    #region Private Helper Methods

    private static string GenerateUnsubscribeToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private async Task SendWelcomeEmailAsync(NewsletterSubscriber subscriber)
    {
        var subject = "Ласкаво просимо до LeveLEO! 🎸";

        var unsubscribeLink = $"https://leveleo.com/newsletter/unsubscribe?token={subscriber.UnsubscribeToken}";

        var replacements = new Dictionary<string, string>
        {
            { "{{UNSUBSCRIBE_LINK}}", unsubscribeLink }
        };

        var body = await _templateService.GetTemplateAsync("NewsletterWelcome", replacements);

        await _emailSender.SendEmailAsync(subscriber.Email, subject, body);
    }

    private async Task SendNewProductEmailAsync(NewsletterSubscriber subscriber, NewProductAnnouncementDto product)
    {
        var subject = $"🎉 Новинка в LeveLEO: {product.ProductName}";

        var productLink = $"https://leveleo.com/products/{product.ProductSlug}";
        var unsubscribeLink = $"https://leveleo.com/newsletter/unsubscribe?token={subscriber.UnsubscribeToken}";

        var replacements = new Dictionary<string, string>
        {
            { "{{PRODUCT_NAME}}", product.ProductName },
            { "{{PRODUCT_PRICE}}", $"{product.Price:C}" },
            { "{{PRODUCT_DESCRIPTION}}", product.ShortDescription ?? "Переглянути деталі на сайті" },
            { "{{PRODUCT_IMAGE}}", product.MainImageUrl ?? "https://leveleo.com/images/placeholder.jpg" },
            { "{{PRODUCT_LINK}}", productLink },
            { "{{UNSUBSCRIBE_LINK}}", unsubscribeLink }
        };

        var body = await _templateService.GetTemplateAsync("NewsletterNewProduct", replacements);

        await _emailSender.SendEmailAsync(subscriber.Email, subject, body);
    }

    private async Task SendNewPromotionEmailAsync(NewsletterSubscriber subscriber, NewPromotionAnnouncementDto promotion)
    {
        var subject = $"🔥 Нова акція в LeveLEO: {promotion.PromotionName}";

        var promotionLink = "https://leveleo.com/promotions";
        var unsubscribeLink = $"https://leveleo.com/newsletter/unsubscribe?token={subscriber.UnsubscribeToken}";

        // Форматувати знижку в залежності від типу
        var discountDisplay = promotion.DiscountType == "Percentage" 
            ? $"{promotion.DiscountValue}%" 
            : $"{promotion.DiscountValue:C}";

        var discountLabel = promotion.DiscountType == "Percentage" 
            ? "Знижка до:" 
            : "Знижка:";

        // Додати інформацію про купон якщо є
        var couponInfo = !string.IsNullOrEmpty(promotion.CouponCode)
            ? $"<p style='font-size: 16px; color: #666; background: #fff7ed; padding: 15px; border-radius: 8px; margin-top: 15px;'>💳 <strong>Промокод:</strong> <span style='font-family: monospace; font-size: 18px; color: #ef4444;'>{promotion.CouponCode}</span></p>"
            : "";

        var replacements = new Dictionary<string, string>
        {
            { "{{PROMOTION_NAME}}", promotion.PromotionName },
            { "{{PROMOTION_DESCRIPTION}}", promotion.Description ?? "Переглянути деталі на сайті" },
            { "{{DISCOUNT_LABEL}}", discountLabel },
            { "{{DISCOUNT_VALUE}}", discountDisplay },
            { "{{VALID_UNTIL}}", promotion.EndDate.ToString("dd.MM.yyyy") },
            { "{{COUPON_INFO}}", couponInfo },
            { "{{PROMOTION_LINK}}", promotionLink },
            { "{{UNSUBSCRIBE_LINK}}", unsubscribeLink }
        };

        var body = await _templateService.GetTemplateAsync("NewsletterNewPromotion", replacements);

        await _emailSender.SendEmailAsync(subscriber.Email, subject, body);
    }

    #endregion
}
