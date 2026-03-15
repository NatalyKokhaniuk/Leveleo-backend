using LeveLEO.Data;
using LeveLEO.Features.Products.Models;
using LeveLEO.Features.Promotions.DTO;
using LeveLEO.Features.Promotions.Models;
using LeveLEO.Features.ShoppingCarts.DTO;
using LeveLEO.Infrastructure.Events;
using LeveLEO.Infrastructure.Events.DomainEvents;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Features.Promotions.Services;

public class PromotionService(AppDbContext db, IEventBus eventBus) : IPromotionService
{
    private readonly AppDbContext _db = db;
    private readonly IEventBus _eventBus = eventBus;

    #region CRUD

    public async Task<PromotionResponseDto> CreateAsync(CreatePromotionDto dto)
    {
        if (await _db.Promotions.AnyAsync(p => p.Slug == dto.Slug))
            throw new ApiException("PROMOTION_SLUG_EXISTS",
                $"Promotion with slug '{dto.Slug}' already exists.", 400);

        if (dto.StartDate >= dto.EndDate)
            throw new ApiException("INVALID_DATES",
                "StartDate must be earlier than EndDate.", 400);

        var promotion = new Promotion
        {
            Name = dto.Name,
            Slug = dto.Slug,
            Description = dto.Description,
            ImageKey = dto.ImageKey,
            Level = dto.Level,
            ProductConditions = dto.ProductConditions,
            CartConditions = dto.CartConditions,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsCoupon = dto.IsCoupon,
            IsPersonal = dto.IsPersonal,
            CouponCode = dto.CouponCode,
            MaxUsages = dto.MaxUsages,
            Translations = dto.Translations?.Select(t => new PromotionTranslation
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Description = t.Description
            }).ToList() ?? []
        };

        _db.Promotions.Add(promotion);
        await _db.SaveChangesAsync();
        await _eventBus.PublishAsync(new PromotionCreatedEvent
        {
            PromotionId = promotion.Id,
            PromotionName = promotion.Name,
            Description = promotion.Description,
            ImageKey = promotion.ImageKey,
            DiscountValue = promotion.DiscountValue ?? 0,
            DiscountType = promotion.DiscountType?.ToString() ?? "Percentage",
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            CouponCode = promotion.CouponCode
        });
        return MapToDto(promotion);
    }

    public async Task<PromotionResponseDto> GetBySlugAsync(string slug)
    {
        var promotion = await _db.Promotions
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Slug == slug)
            ?? throw new ApiException("PROMOTION_NOT_FOUND",
                $"Promotion with slug '{slug}' not found.", 404);

        return MapToDto(promotion);
    }

    public async Task<PromotionResponseDto> UpdateAsync(Guid id, UpdatePromotionDto dto)
    {
        var promotion = await _db.Promotions
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new ApiException("PROMOTION_NOT_FOUND", "", 404);

        if (dto.Name.HasValue)
            promotion.Name = dto.Name.Value!;

        if (dto.Description.HasValue)
            promotion.Description = dto.Description.Value;

        if (dto.ImageKey.HasValue)
            promotion.ImageKey = dto.ImageKey.Value;

        if (dto.Level.HasValue)
            promotion.Level = dto.Level.Value;

        if (dto.ProductConditions.HasValue)
            promotion.ProductConditions = dto.ProductConditions.Value;

        if (dto.CartConditions.HasValue)
            promotion.CartConditions = dto.CartConditions.Value;

        if (dto.DiscountType.HasValue)
            promotion.DiscountType = dto.DiscountType.Value;

        if (dto.DiscountValue.HasValue)
            promotion.DiscountValue = dto.DiscountValue.Value;

        if (dto.StartDate.HasValue)
            promotion.StartDate = dto.StartDate.Value;

        if (dto.EndDate.HasValue)
            promotion.EndDate = dto.EndDate.Value;

        if (dto.IsCoupon.HasValue)
            promotion.IsCoupon = dto.IsCoupon.Value;

        if (dto.IsPersonal.HasValue)
            promotion.IsPersonal = dto.IsPersonal.Value;

        if (dto.CouponCode.HasValue)
            promotion.CouponCode = dto.CouponCode.Value;

        if (dto.MaxUsages.HasValue)
            promotion.MaxUsages = dto.MaxUsages.Value;

        await _db.SaveChangesAsync();

        return MapToDto(promotion);
    }

    private static PromotionResponseDto MapToDto(Promotion p)
    {
        return new PromotionResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            Description = p.Description,
            ImageKey = p.ImageKey,
            Level = p.Level,
            DiscountType = p.DiscountType,
            DiscountValue = p.DiscountValue,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            IsActive = p.IsActive,
            IsCoupon = p.IsCoupon,
            IsPersonal = p.IsPersonal,
            Translations = [.. p.Translations.Select(t => new PromotionTranslationDto
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Description = t.Description
            })]
        };
    }

    public async Task<PromotionResponseDto> GetByIdAsync(Guid id)
    {
        var promotion = await _db.Promotions
                .Include(p => p.Translations)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new ApiException("PROMOTION_NOT_FOUND",
                    $"Promotion with id '{id}' not found.", 404);

        return MapToDto(promotion);
    }

    public async Task<List<PromotionResponseDto>> GetActiveAsync()
    {
        var promotions = await _db.Promotions
        .Where(p => p.IsActive)
        .ToListAsync();

        return [.. promotions.Select(p => new PromotionResponseDto
        {
            Id = p.Id,
            Slug = p.Slug,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            IsActive = p.IsActive
        })];
    }

    public async Task DeleteAsync(Guid id)
    {
        var promotion = await _db.Promotions.FindAsync(id)
            ?? throw new KeyNotFoundException("Promotion not found");

        _db.Promotions.Remove(promotion);
        await _db.SaveChangesAsync();
    }

    #endregion CRUD

    #region PRODUCT LEVEL LOGIC

    public async Task<(decimal?, Promotion?)>
        GetBestProductPromotionAsync(Product product)
    {
        var dict = await GetBestProductPromotionsAsync([product]);

        return dict.TryGetValue(product.Id, out var value)
            ? value
            : (null, null);
    }

    public async Task<Dictionary<Guid, (decimal?, Promotion?)>>
        GetBestProductPromotionsAsync(IEnumerable<Product> products)
    {
        var productList = products.ToList();

        if (productList.Count == 0)
            return [];

        var now = DateTimeOffset.UtcNow;

        var promotions = await _db.Promotions
            .Where(p =>
                p.Level == PromotionLevel.Product &&
                p.StartDate <= now &&
                p.EndDate >= now &&
                p.DiscountType != null &&
                p.DiscountValue != null &&
                p.ProductConditions != null)
            .ToListAsync();

        var result = new Dictionary<Guid, (decimal?, Promotion?)>();

        foreach (var product in productList)
        {
            Promotion? bestPromotion = null;
            decimal bestPrice = product.Price;

            foreach (var promo in promotions)
            {
                if (!IsProductMatchingFast(promo, product))
                    continue;

                var discounted = CalculateDiscount(product.Price, promo);

                if (discounted < bestPrice)
                {
                    bestPrice = discounted;
                    bestPromotion = promo;
                }
            }

            if (bestPromotion != null)
                result[product.Id] = (bestPrice, bestPromotion);
        }

        return result;
    }

    #endregion PRODUCT LEVEL LOGIC

    #region CART LEVEL LOGIC

    public async Task<ShoppingCartPromotionResultDto> ApplyCartPromotionAsync(
        IEnumerable<ShoppingCartItemDto> items,
        string? couponCode = null,
        string? userId = null)
    {
        var itemList = items.ToList();
        var now = DateTimeOffset.UtcNow;

        var result = new ShoppingCartPromotionResultDto
        {
            Items = itemList,
            TotalProductsPrice = itemList.Sum(i => i.PriceAfterProductPromotion * i.Quantity)
        };

        var cartPromotions = await _db.Promotions
            .Where(p =>
                p.Level == PromotionLevel.Cart &&
                p.StartDate <= now &&
                p.EndDate >= now &&
                p.DiscountType != null &&
                p.DiscountValue != null)
            .Include(p => p.Translations)
            .ToListAsync();

        Promotion? couponPromotion = null;
        ApplyCouponResult couponResult = ApplyCouponResult.None;

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            couponPromotion = cartPromotions.FirstOrDefault(p => p.IsCoupon && p.CouponCode == couponCode);

            if (couponPromotion == null)
            {
                couponResult = ApplyCouponResult.Invalid;
            }
            else if (couponPromotion.IsPersonal)
            {
                if (userId == null)
                {
                    couponResult = ApplyCouponResult.NotEligible;
                    couponPromotion = null;
                }
                else
                {
                    var assignment = await _db.CouponAssignments
                        .FirstOrDefaultAsync(ca =>
                            ca.PromotionId == couponPromotion.Id &&
                            ca.UserId == userId &&
                            (!ca.ExpiresAt.HasValue || ca.ExpiresAt > now) &&
                            (ca.MaxUsagePerUser == null || ca.UsageCount < ca.MaxUsagePerUser));

                    if (assignment == null)
                    {
                        couponResult = ApplyCouponResult.NotEligible;
                        couponPromotion = null;
                    }
                }
            }
        }

        var eligiblePromotions = cartPromotions
            .Where(p =>
            {
                if (p == couponPromotion || !p.IsCoupon)
                {
                    var cond = p.CartConditions; // CartLevelCondition
                    var cartTotal = itemList.Sum(i => i.PriceAfterProductPromotion * i.Quantity);
                    var totalQty = itemList.Sum(i => i.Quantity);
                    if (cond != null)
                    {
                        if (cond.MinTotalAmount.HasValue && cartTotal < cond.MinTotalAmount.Value)
                            return false;

                        if (cond.MinQuantity.HasValue && totalQty < cond.MinQuantity.Value)
                            return false;

                        if (cond.ProductIds.HasValue)
                        {
                            var productIds = cond.ProductIds.Value;
                            if (productIds == null || productIds.Count == 0)
                            {
                                return true;
                            }

                            if (!itemList.Any(i => productIds.Contains(i.Product.Id)))
                                return false;
                        }

                        if (cond.CategoryIds.HasValue)
                        {
                            var categoryIds = cond.CategoryIds.Value;
                            if (categoryIds == null || categoryIds.Count == 0)
                            {
                                return true;
                            }

                            if (!itemList.Any(i => categoryIds.Contains(i.Product.CategoryId)))
                                return false;
                        }
                    }

                    return true;
                }
                return false;
            })
            .ToList();

        var bestPromotionResult = eligiblePromotions
            .Select(p =>
            {
                var cartTotal = itemList.Sum(i => i.PriceAfterProductPromotion * i.Quantity);
                var totalDiscount = p.DiscountType switch
                {
                    DiscountType.Percentage => cartTotal * (p.DiscountValue!.Value / 100m),
                    DiscountType.FixedAmount => Math.Min(cartTotal, p.DiscountValue!.Value),
                    _ => 0m
                };
                return new { Promotion = p, DiscountAmount = totalDiscount };
            })
            .OrderByDescending(d => d.DiscountAmount)
            .FirstOrDefault();

        if (bestPromotionResult != null)
        {
            var applied = bestPromotionResult.Promotion;
            var cartTotal = itemList.Sum(i => i.PriceAfterProductPromotion * i.Quantity);
            decimal totalCartDiscount = bestPromotionResult.DiscountAmount;

            foreach (var item in itemList)
            {
                var itemBase = item.PriceAfterProductPromotion * item.Quantity;
                item.PriceAfterCartPromotion = item.PriceAfterProductPromotion;

                if (cartTotal > 0)
                {
                    var proportionalDiscount = totalCartDiscount * (itemBase / cartTotal);
                    item.PriceAfterCartPromotion -= proportionalDiscount / item.Quantity;
                }
            }

            result.TotalCartDiscount = totalCartDiscount;
            result.FinalPrice = itemList.Sum(i => i.PriceAfterCartPromotion * i.Quantity);

            result.AppliedCartPromotion = new AppliedPromotionDto
            {
                Id = applied.Id,
                Slug = applied.Slug,
                Name = applied.Name,
                Description = applied.Description,
                ImageKey = applied.ImageKey,
                Level = applied.Level,
                DiscountType = applied.DiscountType!.Value,
                DiscountValue = applied.DiscountValue!.Value,
                StartDate = applied.StartDate,
                EndDate = applied.EndDate,
                IsCoupon = applied.IsCoupon,
                IsPersonal = applied.IsPersonal,
                CouponCode = applied.CouponCode,
                Translations = [.. applied.Translations
                    .Select(t => new PromotionTranslationDto
                    {
                        LanguageCode = t.LanguageCode,
                        Name = t.Name,
                        Description = t.Description
                    })],
            };

            if (couponPromotion != null && applied.Id == couponPromotion.Id)
            {
                couponResult = ApplyCouponResult.Applied;
            }
            else if (couponPromotion != null)
            {
                couponResult = ApplyCouponResult.BetterPromotionExists;
            }
        }
        else
        {
            result.TotalCartDiscount = 0m;
            result.FinalPrice = itemList.Sum(i => i.PriceAfterProductPromotion * i.Quantity);

            if (couponPromotion != null)
                couponResult = ApplyCouponResult.BetterPromotionExists;
        }

        result.CouponResult = couponResult;

        return result;
    }

    #endregion CART LEVEL LOGIC

    #region Helpers

    private static decimal CalculateDiscount(decimal price, Promotion promo)
    {
        if (promo.DiscountType == null || promo.DiscountValue == null)
            return price;

        return promo.DiscountType switch
        {
            DiscountType.Percentage =>
                price - (price * promo.DiscountValue.Value / 100m),

            DiscountType.FixedAmount =>
                Math.Max(0, price - promo.DiscountValue.Value),

            _ => price
        };
    }

    private static bool IsProductMatchingFast(
    Promotion promo,
    Product product)
    {
        var cond = promo.ProductConditions;
        if (cond == null)
            return false;

        // Якщо задані конкретні продукти
        if (cond.ProductIds.HasValue &&
            cond.ProductIds.Value != null &&
            cond.ProductIds.Value.Count > 0 &&
            !cond.ProductIds.Value.Contains(product.Id))
        {
            return false;
        }

        // Якщо задані категорії
        if (cond.CategoryIds.HasValue &&
            cond.CategoryIds.Value != null &&
            cond.CategoryIds.Value.Count > 0 &&
            !cond.CategoryIds.Value.Contains(product.CategoryId))
        {
            return false;
        }

        return true;
    }

    #endregion Helpers

    #region Translation CRUD

    public async Task<PromotionTranslationDto> AddTranslationAsync(Guid promotionId, PromotionTranslationDto dto)
    {
        var promotion = await _db.Promotions
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == promotionId)
            ?? throw new KeyNotFoundException($"Promotion with Id '{promotionId}' not found.");

        if (promotion.Translations.Any(t => t.LanguageCode == dto.LanguageCode))
            throw new InvalidOperationException($"Translation for language '{dto.LanguageCode}' already exists.");

        var translation = new PromotionTranslation
        {
            PromotionId = promotionId,
            LanguageCode = dto.LanguageCode,
            Name = dto.Name,
            Description = dto.Description
        };

        promotion.Translations.Add(translation);
        await _db.SaveChangesAsync();

        return new PromotionTranslationDto
        {
            LanguageCode = translation.LanguageCode,
            Name = translation.Name,
            Description = translation.Description
        };
    }

    public async Task<PromotionTranslationDto> UpdateTranslationAsync(Guid promotionId, PromotionTranslationDto dto)
    {
        var translation = await _db.PromotionTranslations
            .FirstOrDefaultAsync(t => t.PromotionId == promotionId && t.LanguageCode == dto.LanguageCode)
            ?? throw new KeyNotFoundException($"Translation for language '{dto.LanguageCode}' not found for promotion '{promotionId}'.");

        translation.Name = dto.Name;
        translation.Description = dto.Description;

        await _db.SaveChangesAsync();

        return new PromotionTranslationDto
        {
            LanguageCode = translation.LanguageCode,
            Name = translation.Name,
            Description = translation.Description
        };
    }

    public async Task DeleteTranslationAsync(Guid promotionId, string languageCode)
    {
        var translation = await _db.PromotionTranslations
            .FirstOrDefaultAsync(t => t.PromotionId == promotionId && t.LanguageCode == languageCode)
            ?? throw new KeyNotFoundException($"Translation for language '{languageCode}' not found for promotion '{promotionId}'.");

        _db.PromotionTranslations.Remove(translation);
        await _db.SaveChangesAsync();
    }

    #endregion Translation CRUD
}
