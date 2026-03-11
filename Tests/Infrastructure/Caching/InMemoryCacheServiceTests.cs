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
        // Arrange
        var key = "test-key";
        var value = "test-value";
        await _cacheService.SetAsync(key, value);

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenKeyNotExists_ShouldReturnNull()
    {
        // Arrange
        var key = "non-existent-key";

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ShouldStoreValue()
    {
        // Arrange
        var key = "new-key";
        var value = new { Name = "Test", Count = 42 };

        // Act
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));

        // Assert
        var cached = await _cacheService.GetAsync<dynamic>(key);
        Assert.NotNull(cached);
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteKey()
    {
        // Arrange
        var key = "key-to-delete";
        await _cacheService.SetAsync(key, "value");

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        var result = await _cacheService.GetAsync<string>(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ShouldReturnTrue()
    {
        // Arrange
        var key = "existing-key";
        await _cacheService.SetAsync(key, "value");

        // Act
        var exists = await _cacheService.ExistsAsync(key);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyNotExists_ShouldReturnFalse()
    {
        // Arrange
        var key = "non-existing-key";

        // Act
        var exists = await _cacheService.ExistsAsync(key);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheMiss_ShouldCallFactory()
    {
        // Arrange
        var key = "factory-key";
        var factoryCalled = false;
        var factoryValue = "factory-result";

        Task<string> Factory()
        {
            factoryCalled = true;
            return Task.FromResult(factoryValue);
        }

        // Act
        var result = await _cacheService.GetOrSetAsync(key, Factory);

        // Assert
        Assert.True(factoryCalled);
        Assert.Equal(factoryValue, result);

        // Перевіряємо що значення закешувалось
        var cached = await _cacheService.GetAsync<string>(key);
        Assert.Equal(factoryValue, cached);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheHit_ShouldNotCallFactory()
    {
        // Arrange
        var key = "cached-factory-key";
        var cachedValue = "cached-value";
        await _cacheService.SetAsync(key, cachedValue);

        var factoryCalled = false;
        Task<string> Factory()
        {
            factoryCalled = true;
            return Task.FromResult("should-not-be-called");
        }

        // Act
        var result = await _cacheService.GetOrSetAsync(key, Factory);

        // Assert
        Assert.False(factoryCalled);
        Assert.Equal(cachedValue, result);
    }
}
