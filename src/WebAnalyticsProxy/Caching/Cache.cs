using Microsoft.Extensions.Caching.Memory;

namespace BenjaminAbt.WebAnalyticsProxy.Caching;

/// <summary>
/// Represents a cache abstraction for storing and retrieving analytics data.
/// </summary>
public interface IWebAnalyticsProxyCache
{
    /// <summary>
    /// Retrieves an item from the cache if it exists; otherwise, creates and stores the item using the specified factory function.
    /// </summary>
    /// <typeparam name="TItem">The type of the item to retrieve or create.</typeparam>
    /// <param name="cacheKey">The unique key for the cached item.</param>
    /// <param name="factory">
    /// A function that generates the item to cache. This function is invoked if the key is not found in the cache.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is the cached or newly created item, or null if the factory returns null.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="cacheKey"/> or <paramref name="factory"/> is null.
    /// </exception>
    Task<TItem?> GetOrCreateAsync<TItem>(string cacheKey, Func<ICacheEntry, Task<TItem>> factory);

    /// <summary>
    /// Retrieves an item from the cache using the specified key.
    /// </summary>
    /// <typeparam name="TItem">The type of the item to retrieve from the cache.</typeparam>
    /// <param name="cacheKey">The unique key used to identify the cached item.</param>
    /// <returns>
    /// The cached item if it exists and is of type <typeparamref name="TItem"/>; 
    /// otherwise, the default value of <typeparamref name="TItem"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="cacheKey"/> is null.
    /// </exception>
    /// <remarks>
    /// This method attempts to retrieve an item from the cache by its key. If the item is not 
    /// found or cannot be cast to the specified type, the default value for <typeparamref name="TItem"/> is returned.
    /// </remarks>
    TItem? Get<TItem>(string cacheKey);

    /// <summary>
    /// Sets a value in the memory cache with the specified expiration policy.
    /// </summary>
    /// <param name="cacheKey">The unique key used to identify the cached item.</param>
    /// <param name="value">The value to store in the cache.</param>
    /// <param name="absoluteExpiration">
    /// The time interval after which the cached item will expire and be removed from the cache.
    /// </param>
    /// <returns>
    /// The stored <paramref name="value"/> if successfully cached.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="cacheKey"/> or <paramref name="value"/> is null.
    /// </exception>
    /// <remarks>
    /// This method stores a string value in the memory cache with an absolute expiration time.
    /// If the key already exists, it will overwrite the value with the new one provided.
    /// </remarks>
    string Set(string cacheKey, string value, TimeSpan absoluteExpiration);
}

/// <summary>
/// A memory cache implementation of the <see cref="IWebAnalyticsProxyCache"/> interface using <see cref="IMemoryCache"/>.
/// </summary>
public class WebAnalyticsProxyMemoryCache : IWebAnalyticsProxyCache
{
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebAnalyticsProxyMemoryCache"/> class.
    /// </summary>
    /// <param name="memoryCache">The <see cref="IMemoryCache"/> instance used for caching items.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="memoryCache"/> is null.
    /// </exception>
    public WebAnalyticsProxyMemoryCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    /// <inheritdoc />
    public Task<TItem?> GetOrCreateAsync<TItem>(string cacheKey, Func<ICacheEntry, Task<TItem>> factory)
        // Use the memory cache to retrieve or create the item
        => _memoryCache.GetOrCreateAsync(cacheKey, factory);

    /// <inheritdoc />
    public TItem? Get<TItem>(string cacheKey)
        => (TItem?)(_memoryCache.Get(cacheKey) ?? default(TItem));

    /// <inheritdoc />
    public string Set(string cacheKey, string value, TimeSpan absoluteExpiration)
        => _memoryCache.Set(cacheKey, value, absoluteExpiration);

    public static WebAnalyticsProxyMemoryCache CreateWithMemoryCache()
        => new(new MemoryCache(new MemoryCacheOptions()));
}
