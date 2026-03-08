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

    Task<PromotionResponseDto> GetByIdAsync(Guid id);

    Task<PromotionResponseDto> GetBySlugAsync(string slug);

    Task<List<PromotionResponseDto>> GetActiveAsync();

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

    //===== TRANSLATIONS =====
    Task<PromotionTranslationDto> AddTranslationAsync(Guid promotionId, PromotionTranslationDto dto);

    Task<PromotionTranslationDto> UpdateTranslationAsync(Guid promotionId, PromotionTranslationDto dto);

    Task DeleteTranslationAsync(Guid promotionId, string languageCode);
}