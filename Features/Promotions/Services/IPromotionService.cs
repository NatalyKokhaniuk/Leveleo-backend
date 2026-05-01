using LeveLEO.Features.Products.Models;
using LeveLEO.Features.Promotions.DTO;
using LeveLEO.Features.Promotions.Models;
using LeveLEO.Features.ShoppingCarts.DTO;

namespace LeveLEO.Features.Promotions.Services;

public interface IPromotionService
{
    // CRUD
    Task<PromotionResponseDto> CreateAsync(CreatePromotionDto dto);

    Task<PromotionResponseDto> UpdateAsync(Guid id, UpdatePromotionDto dto);

    Task DeleteAsync(Guid id);

    Task<PromotionResponseDto> GetByIdAsync(Guid id, bool includeSensitiveCouponFields = false);

    Task<PromotionResponseDto> GetBySlugAsync(string slug, bool includeSensitiveCouponFields = false);

    /// <param name="guestEligibleOnly">Якщо true — лише акції без купона та не персональні (доступні будь-кому, у т.ч. без реєстрації).</param>
    Task<List<PromotionResponseDto>> GetActiveAsync(bool guestEligibleOnly = false);

    Task<List<PromotionResponseDto>> GetAllAsync();

    /// <summary>При видаленні товару: прибрати його ID з умов акцій і за потреби обнулити порожні умови.</summary>
    Task RemoveProductFromAllPromotionConditionsAsync(Guid productId);

    // ===== PRODUCT LEVEL =====

    Task<(decimal? discountedPrice, Promotion? promotion)>
        GetBestProductPromotionAsync(Product product);

    Task<Dictionary<Guid, (decimal? discountedPrice, Promotion? promotion)>>
        GetBestProductPromotionsAsync(IEnumerable<Product> products);

    // ===== SHOPPING CART LEVEL =====
    Task<ShoppingCartPromotionResultDto> ApplyCartPromotionAsync(
    IEnumerable<ShoppingCartItemDto> items,
    string? couponCode = null,
    string? userId = null);

    /// <summary>
    /// Після успішної оплати: збільшити UsedCount для купонної акції за кодом купона
    /// та/або UsageCount персонального призначення.
    /// </summary>
    Task RecordPromotionUsageByCouponAsync(string? couponCode, string userId);

    //===== TRANSLATIONS =====
    Task<PromotionTranslationDto> AddTranslationAsync(Guid promotionId, PromotionTranslationDto dto);

    Task<PromotionTranslationDto> UpdateTranslationAsync(Guid promotionId, PromotionTranslationDto dto);

    Task DeleteTranslationAsync(Guid promotionId, string languageCode);
}