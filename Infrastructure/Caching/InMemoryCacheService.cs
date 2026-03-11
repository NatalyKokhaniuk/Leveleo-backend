using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LeveLEO.Infrastructure.Caching;

/// <summary>
/// In-Memory реалізація кешування (fallback для Development або якщо Redis недоступний)
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryCacheService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10);

    public InMemoryCacheService(IMemoryCache cache, ILogger<InMemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return Task.FromResult(value);
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
        };

        _cache.Set(key, value, options);
        _logger.LogDebug("Cached key: {Key} with expiration: {Expiration}", key, expiration ?? DefaultExpiration);
        
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Removed cache key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        _logger.LogWarning("RemoveByPatternAsync is not supported in InMemoryCache. Pattern: {Pattern}", pattern);
        // In-memory cache не підтримує видалення по шаблону
        // Можна реалізувати через окремий список ключів, але це складніше
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        var exists = _cache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue(key, out T? cached))
        {
            return cached!;
        }

        var value = await factory();
        await SetAsync(key, value, expiration);
        return value;
    }
}
