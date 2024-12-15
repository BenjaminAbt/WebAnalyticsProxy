namespace BenjaminAbt.WebAnalyticsProxy.Providers.Cloudflare;

/// <summary>
/// Provides configuration options specific to the Cloudflare web analytics proxy.
/// Inherits from <see cref="WebAnalyticsProxyOptions" />.
/// </summary>
public class CloudflareWebAnalyticsProxyOptions : WebAnalyticsProxyOptions
{
    public CloudflareWebAnalyticsProxyOptions()
    {
        ClientSideJavaScriptUrl = "https://static.cloudflareinsights.com/beacon.min.js";
        CollectUrl = "https://cloudflareinsights.com/cdn-cgi/rum";
    }
}
