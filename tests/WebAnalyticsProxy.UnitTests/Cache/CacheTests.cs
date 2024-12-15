using BenjaminAbt.WebAnalyticsProxy.Caching;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace BenjaminAbt.WebAnalyticsProxy.UnitTests.Cache;

public class WebAnalyticsProxyMemoryCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_ShouldCallFactory_AndReturnResult()
    {
        MemoryCache memoryCache = new(new MemoryCacheOptions());
        WebAnalyticsProxyMemoryCache webAnalyticsProxyMemoryCache = new(memoryCache);

        // Arrange
        string cacheKey = "testKey";
        string expectedValue = "TestValue";

        // Act
        string? result = await webAnalyticsProxyMemoryCache
            .GetOrCreateAsync(cacheKey, _ => Task.FromResult(expectedValue));

        // Assert
        Assert.Equal(expectedValue, result);

        object? entry = memoryCache.Get(cacheKey);
        Assert.NotNull(entry);
    }

    [Fact]
    public void Get_ShouldReturnCachedValue_WhenKeyExists()
    {
        MemoryCache memoryCache = new(new MemoryCacheOptions());
        WebAnalyticsProxyMemoryCache webAnalyticsProxyMemoryCache = new(memoryCache);

        // Arrange
        string cacheKey = "testKey";
        string expectedValue = "CachedValue";
        TimeSpan expiration = TimeSpan.FromMinutes(5);

        // Act 1
        string? result1 = webAnalyticsProxyMemoryCache.Get<string>(cacheKey);

        // Assert 1
        Assert.Null(result1);

        // Act 2       
        webAnalyticsProxyMemoryCache.Set(cacheKey, expectedValue, expiration);
        string? result2 = webAnalyticsProxyMemoryCache.Get<string>(cacheKey);

        // Assert 1
        Assert.Equal(expectedValue, result2);
    }

    [Fact]
    public void Get_ShouldReturnDefault_WhenKeyDoesNotExist()
    {
        MemoryCache memoryCache = new(new MemoryCacheOptions());
        WebAnalyticsProxyMemoryCache webAnalyticsProxyMemoryCache = new(memoryCache);

        // Arrange
        string cacheKey = "nonExistentKey";

        // Act
        string? result = webAnalyticsProxyMemoryCache.Get<string>(cacheKey);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Set_ShouldCallSetOnMemoryCache_WithCorrectValues()
    {
        MemoryCache memoryCache = new(new MemoryCacheOptions());
        WebAnalyticsProxyMemoryCache webAnalyticsProxyMemoryCache = new(memoryCache);

        // Arrange
        string cacheKey = "testKey";
        string cacheValue = "TestString";
        TimeSpan expiration = TimeSpan.FromMinutes(5);

        // Act
        webAnalyticsProxyMemoryCache.Set(cacheKey, cacheValue, expiration);

        // Assert
        object? entry = memoryCache.Get(cacheKey);
        Assert.NotNull(entry);
    }
}
