using LeveLEO.Features.Newsletter.DTO;
using LeveLEO.Features.Newsletter.Services;
using LeveLEO.Infrastructure.Events;
using LeveLEO.Infrastructure.Events.DomainEvents;
using Microsoft.Extensions.Logging;

namespace LeveLEO.Features.Newsletter.EventHandlers;

/// <summary>
/// Відправляє розсилку при створенні нового продукту
/// </summary>
public class ProductCreatedNewsletterHandler(
    INewsletterService newsletterService,
    ILogger<ProductCreatedNewsletterHandler> logger) : IEventHandler<ProductCreatedEvent>
{
    public async Task HandleAsync(ProductCreatedEvent @event)
    {
        try
        {
            var announcement = new NewProductAnnouncementDto
            {
                ProductId = @event.ProductId,
                ProductName = @event.ProductName,
                ProductSlug = @event.ProductSlug,
                Price = @event.Price,
                MainImageUrl = @event.MainImageUrl,
                ShortDescription = @event.ShortDescription
            };

            await newsletterService.SendNewProductAnnouncementAsync(announcement);
            
            logger.LogInformation("Newsletter sent for new product {ProductId}", @event.ProductId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send newsletter for product {ProductId}", @event.ProductId);
        }
    }
}

/// <summary>
/// Відправляє розсилку при створенні нової акції
/// </summary>
public class PromotionCreatedNewsletterHandler(
    INewsletterService newsletterService,
    ILogger<PromotionCreatedNewsletterHandler> logger) : IEventHandler<PromotionCreatedEvent>
{
    public async Task HandleAsync(PromotionCreatedEvent @event)
    {
        try
        {
            var announcement = new NewPromotionAnnouncementDto
            {
                PromotionId = @event.PromotionId,
                PromotionName = @event.PromotionName,
                Description = @event.Description,
                ImageKey = @event.ImageKey,
                DiscountValue = @event.DiscountValue,
                DiscountType = @event.DiscountType,
                EndDate = @event.EndDate,
                CouponCode = @event.CouponCode
            };

            await newsletterService.SendNewPromotionAnnouncementAsync(announcement);
            
            logger.LogInformation("Newsletter sent for new promotion {PromotionId}", @event.PromotionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send newsletter for promotion {PromotionId}", @event.PromotionId);
        }
    }
}
