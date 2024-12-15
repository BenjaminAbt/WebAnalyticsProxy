namespace BenjaminAbt.WebAnalyticsProxy;

/// <summary>
/// Provides default values for the web analytics proxy.
/// </summary>
public static class WebAnalyticsProxyDefaults
{
    /// <summary>
    /// The default content type for client-side JavaScript.
    /// </summary>
    public const string ClientScriptContentType = "text/javascript; charset=UTF-8";

    /// <summary>
    /// The default cache time, in seconds, for client-side JavaScript.
    /// </summary>
    public const int ClientScriptDefaultCacheTimeSeconds = 86400;

    /// <summary>
    /// The default value for the Cache-Control header for client-side JavaScript.
    /// </summary>
    public static string ClientScriptCacheControlDefaultHeaderValue = "max-age=" + ClientScriptDefaultCacheTimeSeconds;
}
