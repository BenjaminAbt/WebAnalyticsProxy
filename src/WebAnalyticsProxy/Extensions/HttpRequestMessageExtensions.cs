using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace BenjaminAbt.WebAnalyticsProxy.Extensions;

/// <summary>
/// Extension methods for <see cref="HttpRequestMessage"/> to facilitate copying headers and content
/// from an <see cref="HttpRequest"/>.
/// </summary>
internal static class HttpRequestMessageExtensions
{
    /// <summary>
    /// Copies headers from an <see cref="HttpRequest"/> to an <see cref="HttpRequestMessage"/>, 
    /// with optional exclusions for specified headers.
    /// </summary>
    /// <param name="requestMessage">
    /// The <see cref="HttpRequestMessage"/> to which the headers will be added.
    /// </param>
    /// <param name="request">
    /// The source <see cref="HttpRequest"/> containing the headers to copy.
    /// </param>
    /// <param name="exceptHeaders">
    /// An optional array of header names to exclude from copying. 
    /// Header names are compared case-insensitively.
    /// </param>
    public static void CopyHeadersFrom(this HttpRequestMessage requestMessage, HttpRequest request, string[]? exceptHeaders = null)
    {
        foreach (KeyValuePair<string, StringValues> header in request.Headers)
        {
            if (exceptHeaders?.Contains(header.Key, StringComparer.InvariantCultureIgnoreCase) is true)
            {
                continue;
            }

            requestMessage.Headers.TryAddWithoutValidation(header.Key, [header.Value]);
        }
    }


    /// <summary>
    /// Copies the body and "Content-Type" header from an <see cref="HttpRequest"/> to an <see cref="HttpRequestMessage"/>.
    /// </summary>
    /// <param name="requestMessage">The target <see cref="HttpRequestMessage"/> to which content will be copied.</param>
    /// <param name="request">The source <see cref="HttpRequest"/> from which content will be copied.</param>
    public static void CopyContentFrom(this HttpRequestMessage requestMessage, HttpRequest request)
    {
        // Set the body of the HttpRequestMessage to the stream from the HttpRequest body.
        if (request.Body is not null)
        {
            requestMessage.Content = new StreamContent(request.Body);

            // Check if the "Content-Type" header exists in the HttpRequest.
            if (request.Headers.TryGetValue("Content-Type", out StringValues value))
            {
                string? contentType = value;
                if (contentType is not null)
                {
                    // Set the "Content-Type" header in the HttpRequestMessage content.
                    requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                }
            }
        }
    }
}


