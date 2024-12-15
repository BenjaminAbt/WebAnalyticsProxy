using System.Net;
using BenjaminAbt.WebAnalyticsProxy.Caching;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace BenjaminAbt.WebAnalyticsProxy.UnitTests;
public class WebAnalyticsProxyTests
{
    [Fact]
    public async Task Collect_ShouldCreateProxyRequest_WithCorrectUriAndMethod()
    {
        HttpClient httpClient = Substitute.For<HttpClient>();
        WebAnalyticsProxyOptions webAnalyticsProxyOptions = new()
        {
            CollectUrl = "https://example.com/collect",
            CollectHttpVersion = "2.0"
        };
        IOptions<WebAnalyticsProxyOptions> options = Options.Create(webAnalyticsProxyOptions);
        IWebAnalyticsProxyCache memoryCache = Substitute.For<IWebAnalyticsProxyCache>();
        WebAnalyticsProxy proxyProvider = Substitute.ForPartsOf<WebAnalyticsProxy>(memoryCache, httpClient, options);

        // Arrange
        HttpRequest sourceRequest = Substitute.For<HttpRequest>();
        sourceRequest.Method.Returns("POST");
        sourceRequest.QueryString.Returns(new QueryString("?param=value"));
        sourceRequest.HttpContext.Connection.RemoteIpAddress.Returns(IPAddress.Parse("127.0.0.1"));

        // Act
        HttpResponseMessage response = await proxyProvider.Collect(sourceRequest);

        // Assert
        await proxyProvider.Received(1).InternalSendToProxy(Arg.Is<HttpRequestMessage>(req =>
            req.RequestUri == new Uri("https://example.com/collect?param=value") &&
            req.Method == HttpMethod.Post &&
            req.Version == new Version(2, 0)
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Collect_ShouldForwardIPAddress_WhenEnabled()
    {
        HttpClient httpClient = Substitute.For<HttpClient>();
        WebAnalyticsProxyOptions webAnalyticsProxyOptions = new()
        {
            CollectUrl = "https://example.com/collect",
            CollectHttpVersion = "2.0",
            ForwardRequestIPAddress = true
        };
        IOptions<WebAnalyticsProxyOptions> options = Options.Create(webAnalyticsProxyOptions);
        IWebAnalyticsProxyCache memoryCache = Substitute.For<IWebAnalyticsProxyCache>();
        WebAnalyticsProxy proxyProvider = Substitute.ForPartsOf<WebAnalyticsProxy>(memoryCache, httpClient, options);

        // Arrange
        HttpRequest sourceRequest = Substitute.For<HttpRequest>();
        sourceRequest.Method.Returns("POST");
        sourceRequest.QueryString.Returns(new QueryString("?param=value"));
        sourceRequest.HttpContext.Connection.RemoteIpAddress.Returns(IPAddress.Parse("127.0.0.1"));

        // Act
        HttpResponseMessage response = await proxyProvider.Collect(sourceRequest);

        // Assert
        await proxyProvider.Received(1).InternalSendToProxy(Arg.Is<HttpRequestMessage>(req =>
            req.Headers.Contains("X-Forwarded-For") &&
            req.Headers.GetValues("X-Forwarded-For").Contains("127.0.0.1")
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Collect_ShouldExcludeSpecifiedHeaders()
    {
        HttpClient httpClient = Substitute.For<HttpClient>();
        WebAnalyticsProxyOptions webAnalyticsProxyOptions = new()
        {
            CollectUrl = "https://example.com/collect",
            CollectHttpVersion = "2.0",
            ForwardRequestIPAddress = true,
            CopyHeadersExcept = ["HeaderToExclude"]
        };
        IOptions<WebAnalyticsProxyOptions> options = Options.Create(webAnalyticsProxyOptions);
        IWebAnalyticsProxyCache memoryCache = Substitute.For<IWebAnalyticsProxyCache>();
        WebAnalyticsProxy proxyProvider = Substitute.ForPartsOf<WebAnalyticsProxy>(memoryCache, httpClient, options);

        // Arrange
        HttpRequest sourceRequest = Substitute.For<HttpRequest>();
        sourceRequest.Method.Returns("POST");
        sourceRequest.QueryString.Returns(new QueryString("?param=value"));
        sourceRequest.HttpContext.Connection.RemoteIpAddress.Returns(IPAddress.Parse("127.0.0.1"));
        sourceRequest.Headers.Returns(new HeaderDictionary
            {
                { "Header1", "Value1" },
                { "HeaderToExclude", "ValueToExclude" }
            });

        // Act
        HttpResponseMessage response = await proxyProvider.Collect(sourceRequest);

        // Assert
        await proxyProvider.Received(1)
            .InternalSendToProxy(Arg.Is<HttpRequestMessage>(req =>
            req.Headers.Contains("Header1") &&
            !req.Headers.Contains("HeaderToExclude")
        ), Arg.Any<CancellationToken>());
    }


    /// <summary>
    /// Tests that <see cref="WebAnalyticsProxy.GetClientJavaScript"/> returns JavaScript from the cache if available.
    /// </summary>
    [Fact]
    public async Task GetClientJavaScript_ShouldReturnJavaScript_FromCache()
    {
        HttpClient httpClient = Substitute.For<HttpClient>();
        WebAnalyticsProxyOptions options = new()
        {
            ClientSideJavaScriptUrl = "https://example.com/script.js",
            ClientSideJavaScriptCachingSeconds = 3600,
            CollectUrl = "https://example.com/collect",
            CollectHttpVersion = "2.0",
            ForwardRequestIPAddress = true,
            CopyHeadersExcept = ["HeaderToExclude"]
        };
        IOptions<WebAnalyticsProxyOptions> optionsAccessor = Options.Create(options);
        IWebAnalyticsProxyCache cache = Substitute.For<IWebAnalyticsProxyCache>();
        WebAnalyticsProxy proxy = Substitute.ForPartsOf<WebAnalyticsProxy>(cache, httpClient, optionsAccessor);

        // Arrange
        string expectedJavaScript = "console.log('cached script');";
        cache.GetOrCreateAsync(Arg.Any<string>(), Arg.Any<Func<ICacheEntry, Task<string?>>>())
            .Returns(Task.FromResult<string?>(expectedJavaScript));

        // Act
        string? result = await proxy.GetClientJavaScript();

        // Assert
        Assert.Equal(expectedJavaScript, result);
    }

    /// <summary>
    /// Tests that <see cref="WebAnalyticsProxy.GetClientJavaScript"/> fetches JavaScript from the endpoint when the cache is bypassed.
    /// </summary>
    [Fact]
    public async Task GetClientJavaScript_ShouldFetchJavaScript_WhenCacheIsBypassed()
    {
        HttpClient httpClient = Substitute.For<HttpClient>();
        WebAnalyticsProxyOptions options = new()
        {
            ClientSideJavaScriptUrl = "https://example.com/script.js",
            ClientSideJavaScriptCachingSeconds = 3600,
            CollectUrl = "https://example.com/collect",
            CollectHttpVersion = "2.0",
            ForwardRequestIPAddress = true,
            CopyHeadersExcept = ["HeaderToExclude"]
        };
        IOptions<WebAnalyticsProxyOptions> optionsAccessor = Options.Create(options);
        IWebAnalyticsProxyCache cache = Substitute.For<IWebAnalyticsProxyCache>();
        WebAnalyticsProxy proxy = Substitute.ForPartsOf<WebAnalyticsProxy>(cache, httpClient, optionsAccessor);

        // Arrange
        string expectedJavaScript = "console.log('fetched script');";
        proxy.LoadClientJavaScriptAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>(expectedJavaScript));

        // Act
        string? result = await proxy.GetClientJavaScript(bypassCache: true);

        // Assert
        Assert.Equal(expectedJavaScript, result);
    }

    /// <summary>
    /// Tests that <see cref="WebAnalyticsProxy.GetClientJavaScript"/> returns fallback JavaScript content if the fetch fails and <paramref name="useFallback"/> is <c>true</c>.
    /// </summary>
    [Fact]
    public async Task GetClientJavaScript_ShouldReturnFallback_WhenFetchFails_AndUseFallbackIsTrue()
    {
        HttpClient httpClient = Substitute.For<HttpClient>();
        WebAnalyticsProxyOptions options = new()
        {
            ClientSideJavaScriptUrl = "https://example.com/script.js",
            ClientSideJavaScriptCachingSeconds = 3600,
            CollectUrl = "https://example.com/collect",
            CollectHttpVersion = "2.0",
            ForwardRequestIPAddress = true,
            CopyHeadersExcept = ["HeaderToExclude"]
        };
        IOptions<WebAnalyticsProxyOptions> optionsAccessor = Options.Create(options);
        IWebAnalyticsProxyCache cache = WebAnalyticsProxyMemoryCache.CreateWithMemoryCache();
        WebAnalyticsProxy proxy = Substitute.ForPartsOf<WebAnalyticsProxy>(cache, httpClient, optionsAccessor);

        // Arrange
        proxy.LoadClientJavaScriptAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>(null));
        proxy.ClientJavaScriptFallbackContent.Returns("console.log('fallback script');");

        // Act
        string? result = await proxy.GetClientJavaScript(useFallback: true);

        // Assert
        Assert.Equal("console.log('fallback script');", result);
    }

    /// <summary>
    /// Tests that <see cref="WebAnalyticsProxy.GetClientJavaScript"/> returns <c>null</c> if the fetch fails and <paramref name="useFallback"/> is <c>false</c>.
    /// </summary>
    [Fact]
    public async Task GetClientJavaScript_ShouldReturnNull_WhenFetchFails_AndUseFallbackIsFalse()
    {
        HttpClient httpClient = Substitute.For<HttpClient>();
        WebAnalyticsProxyOptions options = new()
        {
            ClientSideJavaScriptUrl = "https://example.com/script.js",
            ClientSideJavaScriptCachingSeconds = 3600,
            CollectUrl = "https://example.com/collect",
            CollectHttpVersion = "2.0",
            ForwardRequestIPAddress = true,
            CopyHeadersExcept = ["HeaderToExclude"]
        };
        IOptions<WebAnalyticsProxyOptions> optionsAccessor = Options.Create(options);
        IWebAnalyticsProxyCache cache = WebAnalyticsProxyMemoryCache.CreateWithMemoryCache();
        WebAnalyticsProxy proxy = Substitute.ForPartsOf<WebAnalyticsProxy>(cache, httpClient, optionsAccessor);

        // Arrange
        proxy.LoadClientJavaScriptAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>(null));

        // Act
        string? result = await proxy.GetClientJavaScript(useFallback: false);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that <see cref="WebAnalyticsProxy.GetClientJavaScript"/> caches the fetched JavaScript for future requests.
    /// </summary>
    [Fact]
    public async Task GetClientJavaScript_ShouldCacheFetchedJavaScript()
    {
        HttpClient httpClient = Substitute.For<HttpClient>();
        WebAnalyticsProxyOptions options = new()
        {
            ClientSideJavaScriptUrl = "https://example.com/script.js",
            ClientSideJavaScriptCachingSeconds = 3600,
            CollectUrl = "https://example.com/collect",
            CollectHttpVersion = "2.0",
            ForwardRequestIPAddress = true,
            CopyHeadersExcept = ["HeaderToExclude"]
        };
        IOptions<WebAnalyticsProxyOptions> optionsAccessor = Options.Create(options);
        IWebAnalyticsProxyCache cache = Substitute.For<IWebAnalyticsProxyCache>();
        WebAnalyticsProxy proxy = Substitute.ForPartsOf<WebAnalyticsProxy>(cache, httpClient, optionsAccessor);

        // Arrange
        string cacheKey = $"{nameof(WebAnalyticsProxy)}.{nameof(WebAnalyticsProxy.GetClientJavaScript)}:{options.ClientSideJavaScriptUrl}";
        string expectedJavaScript = "console.log('fetched script');";
        cache.GetOrCreateAsync(cacheKey, Arg.Any<Func<ICacheEntry, Task<string?>>>()).Returns(Task.FromResult<string?>(expectedJavaScript));

        // Act
        string? result = await proxy.GetClientJavaScript();

        // Assert
        Assert.Equal(expectedJavaScript, result);

        // Verify that the JavaScript is cached
        await cache.Received(1).GetOrCreateAsync(cacheKey, Arg.Any<Func<ICacheEntry, Task<string?>>>());
    }
}
