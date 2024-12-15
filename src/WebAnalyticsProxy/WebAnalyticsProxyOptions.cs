using System.ComponentModel.DataAnnotations;

namespace BenjaminAbt.WebAnalyticsProxy;

/// <summary>
/// Represents the configuration options for a web analytics proxy.
/// </summary>
public class WebAnalyticsProxyOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to ignore SSL certificate errors.
    /// Default is <c>false</c>.
    /// </summary>
    public bool IgnoreCertificateErrors { get; set; } = false;

    /// <summary>
    /// Gets or sets the URL for the client-side JavaScript file.
    /// </summary>
    [Required]
    public string ClientSideJavaScriptUrl { get; set; } = null!;

    /// <summary>
    /// Gets or sets the caching duration, in seconds, for the client-side JavaScript file.
    /// Default is 43200 seconds (12 hours).
    /// </summary>
    [Required]
    public int ClientSideJavaScriptCachingSeconds { get; set; } = 43200;

    /// <summary>
    /// Gets or sets the content type of the client-side JavaScript file.
    /// Default is <c>text/javascript; charset=UTF-8</c>.
    /// </summary>
    [Required]
    public string ClientSideJavaScriptContentType { get; set; } = "text/javascript; charset=UTF-8";

    /// <summary>
    /// Gets or sets the URL for data collection.
    /// </summary>
    [Required]
    public string CollectUrl { get; set; } = null!;

    /// <summary>
    /// Gets or sets the HTTP version to use when collecting data.
    /// Default is <c>2.0</c>.
    /// </summary>
    [Required]
    public string CollectHttpVersion { get; set; } = "2.0";

    /// <summary>
    /// Gets or sets a value indicating whether to forward the request's IP address.
    /// Default is <c>false</c>.
    /// </summary>
    public bool ForwardRequestIPAddress { get; set; } = false;

    /// <summary>
    /// Gets or sets an array of headers to exclude when copying headers.
    /// Default excludes the <c>Cookie</c> header.
    /// </summary>
    public string[]? CopyHeadersExcept { get; set; } = ["Cookie"];
}

