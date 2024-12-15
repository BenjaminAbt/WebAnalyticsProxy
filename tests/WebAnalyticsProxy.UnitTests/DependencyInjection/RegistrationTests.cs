using BenjaminAbt.WebAnalyticsProxy.Caching;
using BenjaminAbt.WebAnalyticsProxy.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BenjaminAbt.WebAnalyticsProxy.UnitTests.DependencyInjection;

public class WebAnalyticsProxyDependencyInjectionTests
{
    [Fact]
    public void AddWebAnalyticsProxy_ShouldRegisterIWebAnalyticsProxyCache_AsTransient()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        WebAnalyticsProxyBuilder builder = services.AddWebAnalyticsProxy();

        // Assert
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Check if IWebAnalyticsProxyCache resolves correctly
        IWebAnalyticsProxyCache? cache1 = serviceProvider.GetService<IWebAnalyticsProxyCache>();
        IWebAnalyticsProxyCache? cache2 = serviceProvider.GetService<IWebAnalyticsProxyCache>();

        // Ensure they are distinct instances because it's registered as Transient
        Assert.NotNull(builder);
        Assert.NotSame(cache1, cache2);
        Assert.IsType<WebAnalyticsProxyMemoryCache>(cache1);
        Assert.IsType<WebAnalyticsProxyMemoryCache>(cache2);
    }

    [Fact]
    public void AddWebAnalyticsProxy_ShouldReturnBuilder_ForFurtherConfiguration()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        WebAnalyticsProxyBuilder builder = services.AddWebAnalyticsProxy();

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<WebAnalyticsProxyBuilder>(builder);
    }

    [Fact]
    public void MemoryCache_ShouldBeProperlyConfigured_WhenSetInAddWebAnalyticsProxy()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddWebAnalyticsProxy();
        ServiceProvider provider = services.BuildServiceProvider();
        IWebAnalyticsProxyCache? cache = provider.GetService<IWebAnalyticsProxyCache>();

        // Assert
        Assert.NotNull(cache);
        Assert.IsType<WebAnalyticsProxyMemoryCache>(cache);
    }
}
