# WebAnalyticsProxy <a href="https://www.buymeacoffee.com/benjaminabt" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" height="30" ></a>

[![Build](https://github.com/benjaminabt/WebAnalyticsProxy/actions/workflows/ci.yml/badge.svg)](https://github.com/benjaminabt/WebAnalyticsProxy/actions/workflows/ci.yml)

||WebAnalyticsProxy|
|-|-|
|*NuGet*|[![NuGet](https://img.shields.io/nuget/v/BenjaminAbt.WebAnalyticsProxy.svg?logo=nuget&label=BenjaminAbt.WebAnalyticsProxy)](https://www.nuget.org/packages/BenjaminAbt.WebAnalyticsProxy/)|

All [Packages](https://www.nuget.org/packages/BenjaminAbt.WebAnalyticsProxy)  are available for .NET 7, .NET 8 and .NET 9.

---

## Why? 

Many website users quite rightly use adblockers to prevent the proliferation of annoying banners and the like. However, adblockers not only block questionable tracking, but any kind of tracking in general. Adblockers are based on the principle of blocking certain domains or URLs that are known for tracking. This is also the reason why many website operators rely on their own tracking servers to enable tracking. Own tracking is mainly carried out via a separate domain.

In order to be able to use tracking such as Cloudflare Analytics, the solution is to channel the collection of data through your own website: in other words, a proxy. It should be noted that the origin of the data is then displayed incorrectly by the providers because the corresponding X-Forwarding-To headers may not be supported. But this is a compromise that we can live with.


## Usage

The IWebAnalyticsProxy interface is the core of the project, which is instantiated by various implementations that can also be created by the user.
A Cloudflare implementation is currently built in. All specific implementations such as `ICloudflareWebAnalyticsProxy` are derived from `IWebAnalyticsProxy`.


```csharp
// options
CloudflareWebAnalyticsProxyOptions options = new();
IOptions<CloudflareWebAnalyticsProxyOptions> optionsAccessor = Options.Create(options);

// instance dependencies
HttpClient httpClient = httpClientFactory.GetClient("CloudflareAnalytics");
WebAnalyticsProxyMemoryCache cache = WebAnalyticsProxyMemoryCache(new MemoryCache(new MemoryCacheOptions()));

IWebAnalyticsProxy webProxy = new CloudflareWebAnalyticsProxy(cache, optionsAccessor, httpClient);
// or
ICloudflareWebAnalyticsProxy webProxy = new CloudflareWebAnalyticsProxy(cache, optionsAccessor, httpClient)
```

All of this is also offered accordingly as dependency injection, whereby the basic implementation registers the cache and the analytics providers are added with with-methods.

```csharp
// Program.cs

services.AddWebAnalyticsProxy()
            .WithCloudflare();
```

The subsequent use, for example in an ASP.NET Core Controller, is then carried out via the interface and the actions for loading the JavaScript file and the subsequent proxy endpoint for collecting the data.


```csharp
public class TrackController : Controller
{
    // load client javascript
    [HttpGet, Route("track", Name = "Tracking")]
    public async Task<IActionResult> GetClientJavaScript(
        [FromServices] ICloudflareWebAnalyticsProxy webAnalyticsProxy)
    {
        string? clientJavaScript = await webAnalyticsProxy
            .GetClientJavaScript(bypassCache: false, useFallback: true, cancellationToken: default);

        if (clientJavaScript is null)
        {
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            return Ok("");
        }

        Response.Headers.Append("Cache-Control", WebAnalyticsProxyDefaults.ClientScriptCacheControlDefaultHeaderValue);
        return Content(clientJavaScript, WebAnalyticsProxyDefaults.ClientScriptContentType);
    }

    // collect data
    [HttpPost, Route("track")]
    public async Task<IActionResult> CollectData(
        [FromServices] ICloudflareWebAnalyticsProxy webAnalyticsProxy)
    {
        HttpRequest sourceRequest = this.Request;

        HttpResponseMessage? trackResponse = await webAnalyticsProxy.Collect(sourceRequest);
        if (trackResponse is null)
        {
            return NoContent();
        }

        string responseContent = ""; // await response.Content.ReadAsStringAsync();
        string? responseContentType = trackResponse.Content.Headers.ContentType?.ToString();

        // Add CORS headers if missing
        sourceRequest.HttpContext.Response.Headers.AddIfNotExists("Access-Control-Allow-Origin", sourceRequest.Headers.Origin.FirstOrDefault() ?? "*");
        sourceRequest.HttpContext.Response.Headers.AddIfNotExists("Access-Control-Allow-Headers", "content-type");
        sourceRequest.HttpContext.Response.Headers.AddIfNotExists("Access-Control-Allow-Credentials", "true");

        return new ContentResult
        {
            StatusCode = (int)trackResponse.StatusCode,
            Content = responseContent,
            ContentType = responseContentType
        };
    }
}
```

In the case of Cloudflare, the tracking proxy endpoint must now be added to the HTML snippet:

```csharp
// Razor View, e.g. Layout.cshtml

string trackingRoute = Router.Route("Tracking");
// absolute url
string fullUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.Path}{httpContext.Request.QueryString}" + trackingRoute;

<script defer src="@(fullUrl)" data-cf-beacon='{"send":{"to": "@(fullUrl)"},"token": "yourtokenhere"}' asp-append-version="true"></script>
```


## The most important classes and sub-interfaces

| Name | Description |
|-|-|
| WebAnalyticsProxyMemoryCache | Cache that uses the MemoryCache by default to hold the client JavaScript file. Can be realized by own implementations |
| IWebAnalyticsProxy | The interface via which the client JavaScript and the collection of analytics data is carried out and passed on to the provider.
| WebAnalyticsProxyOptions | Options for the proxy |
| ICloudflareWebAnalyticsProxy | The extension of the IWebAnalyticsProxy interface specifically for Cloudflare Web Analytics |
| CloudflareWebAnalyticsProxy | The implementation of the ICloudflareWebAnalyticsProxy interface for Cloudflare Web Analytics |
| CloudflareWebAnalyticsProxyOptions | The options based on WebAnalyticsProxyOptions for Cloudflare Web Analytics |

## Extensions

The entire Analytics Proxy project serves the goal of positive data collection and maximum flexibility. All implementations can be overwritten or extended.


---

[MIT LICENSE](./LICENSE)
