using BenjaminAbt.WebAnalyticsProxy.Caching;
using BenjaminAbt.WebAnalyticsProxy.Providers.Cloudflare;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace BenjaminAbt.WebAnalyticsProxy.DependencyInjection;

/// <summary>
/// A builder class for configuring and registering web analytics proxies in the dependency injection container.
/// </summary>
/// <param name="Services">
/// The <see cref="IServiceCollection"/> used to configure services for web analytics.
/// </param>
public sealed record class WebAnalyticsProxyBuilder(IServiceCollection Services);

/// <summary>
/// Provides extension methods for registering and configuring web analytics proxies in a dependency injection container.
/// </summary>
public static class WebAnalyticsProxyDependencyInjection
{
    /// <summary>
    /// Adds the core services required for web analytics to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the services will be added.</param>
    /// <returns>
    /// A <see cref="WebAnalyticsProxyBuilder"/> instance that can be used to further configure web analytics services.
    /// </returns>
    /// <remarks>This is the default behavior and uses an isolated instance of <see cref="WebAnalyticsProxyMemoryCache"/></remarks> with an isolated instance of <see cref="MemoryCache"/>
    public static WebAnalyticsProxyBuilder AddWebAnalyticsProxy(this IServiceCollection services)
    {
        return AddWebAnalyticsProxy(services, () =>
        {
            MemoryCache memoryCache = new(new MemoryCacheOptions());
            return new WebAnalyticsProxyMemoryCache(memoryCache);
        });
    }

    /// <summary>
    /// Adds the core services required for web analytics to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the services will be added.</param>
    /// <returns>
    /// A <see cref="WebAnalyticsProxyBuilder"/> instance that can be used to further configure web analytics services.
    /// </returns>
    public static WebAnalyticsProxyBuilder AddWebAnalyticsProxy(this IServiceCollection services, Func<WebAnalyticsProxyMemoryCache> customCacheFunc)
    {
        // Create the builder instance with the provided service collection
        WebAnalyticsProxyBuilder builder = new(services);

        // as transient because every provider is registered as singleton and should have it's own cache
        services.AddTransient<IWebAnalyticsProxyCache>(o =>
        {
            return customCacheFunc();
        });

        // Return the builder for further configuration
        return builder;
    }

    /// <summary>
    /// Configures the web analytics proxy to use Cloudflare Web Analytics.
    /// </summary>
    /// <param name="builder">The <see cref="WebAnalyticsProxyBuilder"/> instance to configure.</param>
    /// <param name="options">
    /// An optional configuration action to customize <see cref="CloudflareWebAnalyticsProxyOptions"/>.
    /// If no action is provided, default options will be used.
    /// </param>
    /// <returns>
    /// The updated <see cref="WebAnalyticsProxyBuilder"/> instance to allow method chaining.
    /// </returns>
    /// <remarks>
    /// If <see cref="WithCloudflare"/> is called multiple times, the latest configuration 
    /// for <see cref="CloudflareWebAnalyticsProxyOptions"/> will overwrite any previously registered options.
    /// However, the services <see cref="ICloudflareWebAnalyticsProxy"/> and <see cref="IWebAnalyticsProxy"/> 
    /// will still resolve to the same singleton instances.
    /// </remarks>
    public static WebAnalyticsProxyBuilder WithCloudflare(this WebAnalyticsProxyBuilder builder,
        Action<CloudflareWebAnalyticsProxyOptions>? options = null)
    {
        // Retrieve the IServiceCollection from the builder
        IServiceCollection services = builder.Services;

        // Create default Cloudflare options and invoke the custom configuration if provided
        CloudflareWebAnalyticsProxyOptions cloudflareWebAnalyticsProxyOptions = new();
        options?.Invoke(cloudflareWebAnalyticsProxyOptions);

        // Register the Cloudflare options, Cloudflare-specific proxy, and generic proxy as services
        services.TryAddSingleton(Options.Create(cloudflareWebAnalyticsProxyOptions));
        services.TryAddSingleton<ICloudflareWebAnalyticsProxy, CloudflareWebAnalyticsProxy>();
        services.TryAddSingleton<IWebAnalyticsProxy>(s => s.GetRequiredService<ICloudflareWebAnalyticsProxy>());

        services.AddHttpClient<CloudflareWebAnalyticsProxy>();

        // Return the builder for further chaining
        return builder;
    }
}
