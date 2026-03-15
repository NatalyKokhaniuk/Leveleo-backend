using LeveLEO.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LeveLEO.Tests.Infrastructure.Caching;

public class InMemoryCacheServiceTests
{
    private readonly InMemoryCacheService _cacheService;
    private readonly IMemoryCache _memoryCache;

    public InMemoryCacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        var loggerMock = new Mock<ILogger<InMemoryCacheService>>();
        _cacheService = new InMemoryCacheService(_memoryCache, loggerMock.Object);
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ShouldReturnValue()
    {
        var key = "test-key";
        var value = "test-value";
        await _cacheService.SetAsync(key, value);

        var result = await _cacheService.GetAsync<string>(key);

        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenKeyNotExists_ShouldReturnNull()
    {
        var key = "non-existent-key";

        var result = await _cacheService.GetAsync<string>(key);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ShouldStoreValue()
    {
        var key = "new-key";
        var value = new { Name = "Test", Count = 42 };

        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));

        var cached = await _cacheService.GetAsync<dynamic>(key);
        Assert.NotNull(cached);
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteKey()
    {
        var key = "key-to-delete";
        await _cacheService.SetAsync(key, "value");

        await _cacheService.RemoveAsync(key);

        var result = await _cacheService.GetAsync<string>(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ShouldReturnTrue()
    {
        var key = "existing-key";
        await _cacheService.SetAsync(key, "value");

        var exists = await _cacheService.ExistsAsync(key);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyNotExists_ShouldReturnFalse()
    {
        var key = "non-existing-key";

        var exists = await _cacheService.ExistsAsync(key);

        Assert.False(exists);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheMiss_ShouldCallFactory()
    {
        var key = "factory-key";
        var factoryCalled = false;
        var factoryValue = "factory-result";

        Task<string> Factory()
        {
            factoryCalled = true;
            return Task.FromResult(factoryValue);
        }

        var result = await _cacheService.GetOrSetAsync(key, Factory);

        Assert.True(factoryCalled);
        Assert.Equal(factoryValue, result);

        // Перевірка що значення закешувалось
        var cached = await _cacheService.GetAsync<string>(key);
        Assert.Equal(factoryValue, cached);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheHit_ShouldNotCallFactory()
    {
        var key = "cached-factory-key";
        var cachedValue = "cached-value";
        await _cacheService.SetAsync(key, cachedValue);

        var factoryCalled = false;
        Task<string> Factory()
        {
            factoryCalled = true;
            return Task.FromResult("should-not-be-called");
        }

        var result = await _cacheService.GetOrSetAsync(key, Factory);

        Assert.False(factoryCalled);
        Assert.Equal(cachedValue, result);
    }
}
