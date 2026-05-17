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

    internal static bool IsProductLevelWithoutTargeting(ProductLevelCondition plc)
    {
        bool hasProd = plc.ProductIds.HasValue && plc.ProductIds.Value != null && plc.ProductIds.Value.Count > 0;
        bool hasCat = plc.CategoryIds.HasValue && plc.CategoryIds.Value != null && plc.CategoryIds.Value.Count > 0;
        return !hasProd && !hasCat;
    }

    internal static bool HasProductOrCategoryTargeting(
        Optional<List<Guid>> productIds,
        Optional<List<Guid>> categoryIds)
    {
        return HasNonEmptyIdList(productIds) || HasNonEmptyIdList(categoryIds);
    }

    private static bool HasNonEmptyIdList(Optional<List<Guid>> opt)
        => opt.Value is { Count: > 0 };

    internal static HashSet<Guid> CollectCategoryIdsFromPromotions(IEnumerable<Promotion> promotions)
    {
        var set = new HashSet<Guid>();
        foreach (var promotion in promotions)
        {
            if (promotion.ProductConditions?.CategoryIds.Value is { Count: > 0 } productCats)
            {
                foreach (var id in productCats)
                    set.Add(id);
            }

            if (promotion.CartConditions?.CategoryIds.Value is { Count: > 0 } cartCats)
            {
                foreach (var id in cartCats)
                    set.Add(id);
            }
        }

        return set;
    }

    /// <summary>
    /// Для кожного предка — усі нащадкові categoryId (включно з самим предком).
    /// </summary>
    internal static async Task<Dictionary<Guid, HashSet<Guid>>> BuildCategoryDescendantMapAsync(
        AppDbContext db,
        IEnumerable<Guid> ancestorCategoryIds)
    {
        var ancestorList = ancestorCategoryIds.Distinct().ToList();
        var map = new Dictionary<Guid, HashSet<Guid>>();

        if (ancestorList.Count == 0)
            return map;

        foreach (var ancestorId in ancestorList)
            map[ancestorId] = [];

        var rows = await db.CategoryClosures
            .AsNoTracking()
            .Where(c => ancestorList.Contains(c.AncestorId))
            .Select(c => new { c.AncestorId, c.DescendantId })
            .ToListAsync();

        foreach (var row in rows)
            map[row.AncestorId].Add(row.DescendantId);

        return map;
    }

    internal static bool ProductCategoryMatchesPromotionCategories(
        Guid productCategoryId,
        IReadOnlyList<Guid> promotionCategoryIds,
        IReadOnlyDictionary<Guid, HashSet<Guid>> categoryDescendantMap)
    {
        foreach (var ancestorId in promotionCategoryIds)
        {
            if (categoryDescendantMap.TryGetValue(ancestorId, out var descendants) &&
                descendants.Contains(productCategoryId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Усі productId, на які поширюється акція (явні товари, категорії з підкатегоріями, або весь каталог).
    /// </summary>
    internal static async Task<HashSet<Guid>> ResolvePromotionReferencedProductIdsAsync(
        AppDbContext db,
        Promotion promotion,
        bool activeOnly = false)
    {
        var ids = new HashSet<Guid>();

        if (promotion.Level == PromotionLevel.Product)
        {
            if (promotion.ProductConditions == null)
            {
                await AddAllProductIdsAsync(db, ids, activeOnly);
            }
            else
            {
                await AddResolvedProductIdsAsync(
                    db,
                    ids,
                    promotion.ProductConditions.ProductIds,
                    promotion.ProductConditions.CategoryIds,
                    activeOnly);
            }
        }

        if (promotion.CartConditions != null)
        {
            await AddResolvedProductIdsAsync(
                db,
                ids,
                promotion.CartConditions.ProductIds,
                promotion.CartConditions.CategoryIds,
                activeOnly);
        }
        else if (promotion.Level == PromotionLevel.Cart)
        {
            await AddAllProductIdsAsync(db, ids, activeOnly);
        }

        return ids;
    }

    private static async Task AddResolvedProductIdsAsync(
        AppDbContext db,
        HashSet<Guid> ids,
        Optional<List<Guid>> productIdsOpt,
        Optional<List<Guid>> categoryIdsOpt,
        bool activeOnly)
    {
        if (!HasProductOrCategoryTargeting(productIdsOpt, categoryIdsOpt))
        {
            await AddAllProductIdsAsync(db, ids, activeOnly);
            return;
        }

        if (productIdsOpt.Value is { Count: > 0 } explicitIds)
        {
            foreach (var id in explicitIds)
                ids.Add(id);
        }

        if (categoryIdsOpt.Value is not { Count: > 0 } catIds)
            return;

        var descendantCategoryIds = await db.CategoryClosures
            .AsNoTracking()
            .Where(c => catIds.Contains(c.AncestorId))
            .Select(c => c.DescendantId)
            .Distinct()
            .ToListAsync();

        var productQuery = db.Products
            .AsNoTracking()
            .Where(p => descendantCategoryIds.Contains(p.CategoryId));

        if (activeOnly)
            productQuery = productQuery.Where(p => p.IsActive);

        var categoryProductIds = await productQuery
            .Select(p => p.Id)
            .ToListAsync();

        foreach (var id in categoryProductIds)
            ids.Add(id);
    }

    private static async Task AddAllProductIdsAsync(AppDbContext db, HashSet<Guid> ids, bool activeOnly)
    {
        var query = db.Products.AsNoTracking();
        if (activeOnly)
            query = query.Where(p => p.IsActive);

        var allIds = await query.Select(p => p.Id).ToListAsync();
        foreach (var id in allIds)
            ids.Add(id);
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
