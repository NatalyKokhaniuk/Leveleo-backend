using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace LeveLEO.Infrastructure.Caching;

/// <summary>
/// Redis реалізація кешування
/// </summary>
public class RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10);

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }

            logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting cache key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            var expirationTime = expiration ?? DefaultExpiration;

            await _db.StringSetAsync(key, serialized, expirationTime);
            logger.LogDebug("Cached key: {Key} with expiration: {Expiration}", key, expirationTime);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting cache key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
            logger.LogDebug("Removed cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var endpoints = redis.GetEndPoints();
            var server = redis.GetServer(endpoints.First());

            var keys = server.Keys(pattern: pattern).ToArray();

            if (keys.Length > 0)
            {
                await _db.KeyDeleteAsync(keys);
                logger.LogInformation("Removed {Count} cache keys matching pattern: {Pattern}", keys.Length, pattern);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await _db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if cache key exists: {Key}", key);
            return false;
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        try
        {
            // Спробувати отримати з кешу
            var cached = await GetAsync<T>(key);
            if (cached != null)
            {
                return cached;
            }

            // Якщо немає - виконати фабрику
            var value = await factory();

            // Закешувати результат
            await SetAsync(key, value, expiration);

            return value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetOrSetAsync for key: {Key}", key);
            // Якщо кеш не працює - просто викликаємо фабрику
            return await factory();
        }
    }
}