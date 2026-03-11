using LeveLEO.Features.Products.DTO;
using LeveLEO.Features.Products.Services;
using LeveLEO.Infrastructure.Caching;
using Microsoft.Extensions.Logging;

namespace LeveLEO.Features.Products.Services;

/// <summary>
/// Декоратор для ProductService з кешуванням
/// </summary>
public class CachedProductService(
    IProductService inner,
    ICacheService cache,
    ILogger<CachedProductService> logger) : IProductService
{
    // BUILD DTO методи
    public async Task<ProductResponseDto> BuildFullDtoAsync(Guid productId)
    {
        var key = CacheKeys.Product(productId);

        return await cache.GetOrSetAsync(
            key,
            async () => await inner.BuildFullDtoAsync(productId),
            CacheKeys.Ttl.Product
        );
    }

    public Task<List<ProductResponseDto>> BuildFullDtosAsync(IEnumerable<Guid> productIds)
    {
        // Не кешуємо batch операції
        return inner.BuildFullDtosAsync(productIds);
    }

    // CREATE - invalidate cache
    public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto)
    {
        var result = await inner.CreateAsync(dto);
        await InvalidateProductCacheAsync();
        return result;
    }

    // UPDATE - invalidate cache
    public async Task<ProductResponseDto> UpdateAsync(Guid productId, UpdateProductDto dto)
    {
        var result = await inner.UpdateAsync(productId, dto);
        await cache.RemoveAsync(CacheKeys.Product(productId));
        await cache.RemoveAsync(CacheKeys.ProductBySlug(result.Slug));
        await InvalidateProductCacheAsync();
        return result;
    }

    // DELETE - invalidate cache
    public async Task DeleteAsync(Guid productId)
    {
        var product = await inner.GetByIdAsync(productId);
        await inner.DeleteAsync(productId);
        await cache.RemoveAsync(CacheKeys.Product(productId));
        await cache.RemoveAsync(CacheKeys.ProductBySlug(product.Slug));
        await InvalidateProductCacheAsync();
    }

    // READ - with caching (БЕЗ languageCode - його немає в інтерфейсі!)
    public async Task<ProductResponseDto> GetByIdAsync(Guid productId)
    {
        var key = CacheKeys.Product(productId);

        return await cache.GetOrSetAsync(
            key,
            async () => await inner.GetByIdAsync(productId),
            CacheKeys.Ttl.Product
        );
    }

    public async Task<ProductResponseDto> GetBySlugAsync(string slug)
    {
        var key = CacheKeys.ProductBySlug(slug);

        return await cache.GetOrSetAsync(
            key,
            async () => await inner.GetBySlugAsync(slug),
            CacheKeys.Ttl.Product
        );
    }

    public async Task<PagedResultDto<ProductResponseDto>> GetAllAsync(ProductFilterDto filter)
    {
        // Списки НЕ кешуємо
        return await inner.GetAllAsync(filter);
    }

    public async Task<PagedResultDto<ProductResponseDto>> SearchAsync(string query, int page = 1, int pageSize = 20)
    {
        // Пошук НЕ кешуємо
        return await inner.SearchAsync(query, page, pageSize);
    }

    // TRANSLATIONS - invalidate cache після змін
    public async Task<ProductTranslationResponseDto> AddTranslationAsync(Guid productId, ProductTranslationDto dto)
    {
        var result = await inner.AddTranslationAsync(productId, dto);
        await cache.RemoveAsync(CacheKeys.Product(productId));
        await InvalidateProductCacheAsync();
        return result;
    }

    public async Task<ProductTranslationResponseDto> UpdateTranslationAsync(Guid productId, ProductTranslationDto dto)
    {
        var result = await inner.UpdateTranslationAsync(productId, dto);
        await cache.RemoveAsync(CacheKeys.Product(productId));
        await InvalidateProductCacheAsync();
        return result;
    }

    public async Task DeleteTranslationAsync(Guid productId, string languageCode)
    {
        await inner.DeleteTranslationAsync(productId, languageCode);
        await cache.RemoveAsync(CacheKeys.Product(productId));
        await InvalidateProductCacheAsync();
    }

    private async Task InvalidateProductCacheAsync()
    {
        try
        {
            await cache.RemoveByPatternAsync(CacheKeys.ProductsPattern);
            await cache.RemoveAsync(CacheKeys.FeaturedReviews);
            logger.LogDebug("Product cache invalidated");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error invalidating product cache");
        }
    }
}