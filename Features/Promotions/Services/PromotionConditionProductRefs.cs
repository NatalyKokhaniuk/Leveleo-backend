using LeveLEO.Data;
using LeveLEO.Features.Products.Models;
using LeveLEO.Features.Promotions.Models;
using LeveLEO.Features.Promotions.Models.LevelConditions;
using LeveLEO.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Features.Promotions.Services;

/// <summary>Очищення та перевірка productId у ProductConditions/CartConditions.</summary>
internal static class PromotionConditionProductRefs
{
    internal static HashSet<Guid> CollectProductIds(ProductLevelCondition? plc, CartLevelCondition? clc)
    {
        var set = new HashSet<Guid>();

        AddFromOptional(plc?.ProductIds, set);
        AddFromOptional(clc?.ProductIds, set);

        return set;

        static void AddFromOptional(Optional<List<Guid>>? opt, HashSet<Guid> ids)
        {
            if (opt is not { } o || !o.HasValue || o.Value == null || o.Value.Count == 0)
                return;
            foreach (var id in o.Value)
                ids.Add(id);
        }
    }

    internal static async Task EnsureReferencedProductsExistAsync(AppDbContext db, ProductLevelCondition? plc, CartLevelCondition? clc)
    {
        var ids = CollectProductIds(plc, clc);
        if (ids.Count == 0)
            return;

        var count = await db.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .CountAsync();

        if (count != ids.Count)
        {
            throw new ApiException(
                "PROMOTION_INVALID_PRODUCT_IDS",
                "One or more products referenced in promotion conditions were not found.",
                400);
        }
    }

    /// <returns>True if promotion graph was mutated (needs SaveChanges).</returns>
    internal static bool StripProductIdFromPromotion(Promotion promotion, Guid productId)
    {
        var changed = false;

        if (promotion.ProductConditions != null)
        {
            changed |= RemoveFromProductLevel(promotion.ProductConditions, productId);
            if (IsProductLevelWithoutTargeting(promotion.ProductConditions))
            {
                promotion.ProductConditions = null;
                changed = true;
            }
        }

        if (promotion.CartConditions != null)
        {
            changed |= RemoveFromCartLevel(promotion.CartConditions, productId);
            if (IsCartLevelWithoutAnyCondition(promotion.CartConditions))
            {
                promotion.CartConditions = null;
                changed = true;
            }
        }

        return changed;
    }

    private static bool RemoveFromProductLevel(ProductLevelCondition plc, Guid productId)
    {
        var changed = false;
        if (plc.ProductIds.HasValue && plc.ProductIds.Value != null)
            changed = plc.ProductIds.Value.Remove(productId);
        return changed;
    }

    private static bool RemoveFromCartLevel(CartLevelCondition clc, Guid productId)
    {
        var changed = false;
        if (clc.ProductIds.HasValue && clc.ProductIds.Value != null)
            changed = clc.ProductIds.Value.Remove(productId);
        return changed;
    }

    private static bool IsProductLevelWithoutTargeting(ProductLevelCondition plc)
    {
        bool hasProd = plc.ProductIds.HasValue && plc.ProductIds.Value != null && plc.ProductIds.Value.Count > 0;
        bool hasCat = plc.CategoryIds.HasValue && plc.CategoryIds.Value != null && plc.CategoryIds.Value.Count > 0;
        return !hasProd && !hasCat;
    }

    private static bool IsCartLevelWithoutAnyCondition(CartLevelCondition clc)
    {
        bool hasMinAmount = clc.MinTotalAmount is decimal a && a > 0;
        bool hasMinQty = clc.MinQuantity is int q && q > 0;
        bool hasProd = clc.ProductIds.HasValue && clc.ProductIds.Value != null && clc.ProductIds.Value.Count > 0;
        bool hasCat = clc.CategoryIds.HasValue && clc.CategoryIds.Value != null && clc.CategoryIds.Value.Count > 0;
        return !hasMinAmount && !hasMinQty && !hasProd && !hasCat;
    }
}
