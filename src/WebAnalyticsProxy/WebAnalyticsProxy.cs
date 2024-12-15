using System.Net;
using BenjaminAbt.WebAnalyticsProxy.Caching;
using BenjaminAbt.WebAnalyticsProxy.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BenjaminAbt.WebAnalyticsProxy;

internal static class WebAnalyticsProxyFactory
{
    /// <summary>
    /// Creates an <see cref="HttpRequestMessage"/> from an incoming <see cref="HttpRequest"/>, 
    /// including optional header exclusions and IP address forwarding.
    /// </summary>
    /// <param name="sourceRequest">The source HTTP request.</param>
    /// <param name="proxyUri">The URI to forward the request to.</param>
    /// <param name="httpVersion">The HTTP version to use for the forwarded request.</param>
    /// <param name="forwardIPAddress">Whether to include the originating IP address in the forwarded request.</param>
    /// <param name="exceptHeaders">Headers to exclude from copying.</param>
    /// <returns>A new <see cref="HttpRequestMessage"/> configured for the proxy request.</returns>
    internal static HttpRequestMessage CreateMessageFrom(HttpRequest sourceRequest, Uri proxyUri, Version httpVersion,
        bool forwardIPAddress = false, string[]? exceptHeaders = null)
    {
        // Prepare the target URI with the query string from the source request
        string? queryString = sourceRequest.QueryString.Value;
        UriBuilder uriBuilder = new(proxyUri) { Query = queryString };
        Uri targetUri = uriBuilder.Uri;

        // Create the proxy HTTP request message
        HttpRequestMessage requestMessage = new()
        {
            RequestUri = targetUri,
            Method = new HttpMethod(sourceRequest.Method),
            Version = httpVersion
        };

        // Copy headers from the source request
        requestMessage.CopyHeadersFrom(sourceRequest, exceptHeaders);
        requestMessage.Headers.Host = targetUri.Authority;

        // Optionally forward the client's IP address
        if (forwardIPAddress)
        {
            IPAddress? requestIPAddress = sourceRequest.HttpContext.Connection.RemoteIpAddress;
            requestMessage.Headers.TryAddWithoutValidation("X-Forwarded-For", requestIPAddress?.ToString());
        }

        // Copy content from the source request
        requestMessage.CopyContentFrom(sourceRequest);

        return requestMessage;
    }
}

/// <summary>
/// Defines the contract for a web analytics proxy.
/// Provides methods to retrieve client-side JavaScript and handle data collection requests.
/// </summary>
public interface IWebAnalyticsProxy
{
    /// <summary>
    /// Asynchronously retrieves the client-side JavaScript used for analytics.
    /// Allows options to bypass caching, use fallback content on failure, and monitor cancellation requests.
    /// </summary>
    /// <param name="bypassCache">
    /// Indicates whether to bypass the cache and fetch a fresh copy of the client-side JavaScript directly.
    /// Default is <c>false</c>.
    /// </param>
    /// <param name="useFallback">
    /// Determines whether to use fallback JavaScript content if the retrieval fails.
    /// Default is <c>true</c>.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests, allowing the operation to be gracefully canceled.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task will return the JavaScript content as a string,
    /// or <c>null</c> if retrieval fails and fallback is disabled or no fallback content is available.
    /// </returns>
    Task<string?> GetClientJavaScript(bool bypassCache = false, bool useFallback = true, CancellationToken cancellationToken = default);


    /// <summary>
    /// Handles the data collection request by forwarding it to the analytics endpoint.
    /// </summary>
    /// <param name="request">
    /// The incoming <see cref="HttpRequest"/> containing the data to collect.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the <see cref="HttpResponseMessage"/> 
    /// returned by the analytics endpoint.
    /// </returns>
    Task<HttpResponseMessage> Collect(HttpRequest request, CancellationToken cancellationToken = default);
}


/// <summary>
/// An abstract base class for implementing a web analytics proxy.
/// Provides methods for handling client-side JavaScript retrieval and forwarding analytics data.
/// </summary>
public abstract class WebAnalyticsProxy
{
    /// <summary>
    /// A shared memory cache for caching proxy-related data, such as client-side JavaScript.
    /// </summary>
    private readonly IWebAnalyticsProxyCache _cache;

    /// <summary>
    /// The <see cref="HttpClient"/> used to perform proxy-related HTTP operations.
    /// </summary>
    protected readonly HttpClient ProxyHttpClient;

    /// <summary>
    /// The configuration options for the web analytics proxy.
    /// </summary>
    private readonly WebAnalyticsProxyOptions _options;

    /// <summary>
    /// The cache duration for client-side JavaScript content, based on the options.
    /// </summary>
    private readonly TimeSpan _clientJavaScriptLoadCacheDuration;

    /// <summary>
    /// The URI for forwarding analytics data to the proxy endpoint.
    /// </summary>
    private readonly Uri _proxyCollectUri;

    /// <summary>
    /// The HTTP version to use for proxy requests.
    /// </summary>
    private readonly Version _proxyCollectHttpVersion;

    /// <summary>
    /// Gets fallback content for the client-side JavaScript.
    /// Subclasses can override this to provide custom fallback content.
    /// </summary>
    public virtual string? ClientJavaScriptFallbackContent { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebAnalyticsProxy"/> class.
    /// </summary>
    /// <param name="providerCache">
    /// An <see cref="IWebAnalyticsProxyCache"/> instance used for caching client-side JavaScript and other data.
    /// </param>
    /// <param name="httpClient">
    /// An <see cref="HttpClient"/> instance used to send HTTP requests to the analytics service.
    /// </param>
    /// <param name="options">
    /// An <see cref="IOptions{TOptions}"/> containing configuration settings for the <see cref="WebAnalyticsProxy"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="providerCache"/>, <paramref name="httpClient"/>, or <paramref name="options"/> is null.
    /// </exception>
    /// <remarks>
    /// The constructor initializes key settings such as the caching duration, collection URL, 
    /// and HTTP version based on the provided <see cref="WebAnalyticsProxyOptions"/>.
    /// </remarks>
    public WebAnalyticsProxy(IWebAnalyticsProxyCache providerCache, HttpClient httpClient, IOptions<WebAnalyticsProxyOptions> options)
    {
        _cache = providerCache;
        ProxyHttpClient = httpClient;
        _options = options.Value;

        _clientJavaScriptLoadCacheDuration = TimeSpan.FromSeconds(_options.ClientSideJavaScriptCachingSeconds);
        _proxyCollectUri = new Uri(_options.CollectUrl);
        _proxyCollectHttpVersion = new Version(_options.CollectHttpVersion);
    }


    /// <summary>
    /// Forwards a data collection request to the analytics endpoint.
    /// </summary>
    /// <param name="request">The incoming HTTP request to process.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The response from the analytics endpoint.</returns>
    public virtual Task<HttpResponseMessage> Collect(HttpRequest request, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage proxyRequest = WebAnalyticsProxyFactory.CreateMessageFrom(request, _proxyCollectUri, _proxyCollectHttpVersion,
            _options.ForwardRequestIPAddress, _options.CopyHeadersExcept);

        return InternalSendToProxy(proxyRequest, cancellationToken);
    }

    /// <summary>
    /// Sends the given proxy request using the internal <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="proxyRequest">The proxy request to send.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The HTTP response from the proxy endpoint.</returns>
    internal virtual Task<HttpResponseMessage> InternalSendToProxy(HttpRequestMessage proxyRequest, CancellationToken cancellationToken)
    {
        return ProxyHttpClient.SendAsync(proxyRequest, cancellationToken);
    }

    /// <summary>
    /// Retrieves the client-side JavaScript used for analytics, with options to bypass the cache 
    /// and specify whether to use fallback content in case of failure.
    /// </summary>
    /// <param name="bypassCache">
    /// A value indicating whether to bypass the cache and fetch a fresh copy of the JavaScript.
    /// Default is <c>false</c>.
    /// </param>
    /// <param name="useFallback">
    /// A value indicating whether to use fallback content if the JavaScript cannot be retrieved.
    /// Default is <c>true</c>.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the JavaScript content as a string,
    /// or <c>null</c> if it could not be retrieved and fallback is disabled.
    /// </returns>
    public virtual async Task<string?> GetClientJavaScript(bool bypassCache = false, bool useFallback = true, CancellationToken cancellationToken = default)
    {
        string? data = null;

        // load from endpoint if cache is ignored
        if (bypassCache)
        {

            data = await LoadClientJavaScriptAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Retrieve JavaScript from the cache if available
            string cacheKey = $"{nameof(WebAnalyticsProxy)}.{nameof(GetClientJavaScript)}:{_options.ClientSideJavaScriptUrl}";
            data = await _cache.GetOrCreateAsync(cacheKey, async (cacheEntry) =>
            {
                // load
                string? data = await LoadClientJavaScriptAsync(cancellationToken).ConfigureAwait(false);

                // set cache expiration if we got data
                if (data is not null)
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = _clientJavaScriptLoadCacheDuration;
                    return data;
                }

                // avoid cache and return null
                cacheEntry.Dispose();
                return null;
            }).ConfigureAwait(false);

        }

        // Check if the JavaScript content could not be retrieved and fallback is allowed
        if (data is null && useFallback)
        {
            // Return the fallback JavaScript content if defined
            return ClientJavaScriptFallbackContent;
        }

        // If JavaScript content was successfully retrieved or fallback is not allowed, return the content
        return data;
    }

    /// <summary>
    /// Loads the client-side JavaScript directly from the configured URL.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The JavaScript content, or <c>null</c> if unavailable.</returns>
    public virtual async Task<string?> LoadClientJavaScriptAsync(CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage response = await ProxyHttpClient
            .GetAsync(_options.ClientSideJavaScriptUrl, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            return null;
        }
        catch
        {
            // if the DNS server cannot resolve the host (e.g. has ad filters like PiHole) 
            // > "System.Net.Http.HttpRequestException: 'The requested name is valid, but no data of the requested type was found"
            // occurs

            return null;
        }
    }
}

