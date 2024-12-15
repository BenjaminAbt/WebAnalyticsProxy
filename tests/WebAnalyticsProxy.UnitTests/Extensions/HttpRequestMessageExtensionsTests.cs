using BenjaminAbt.WebAnalyticsProxy.Extensions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace BenjaminAbt.WebAnalyticsProxy.UnitTests.Extensions;

public class CopyHeadersFromTests
{
    [Fact]
    public void CopyHeadersFrom_ShouldCopyAllHeaders_WhenNoExclusionsProvided()
    {
        // Arrange
        HttpRequestMessage requestMessage = new();
        HttpRequest httpRequest = Substitute.For<HttpRequest>();
        httpRequest.Headers.Returns(new HeaderDictionary
        {
            { "Header1", "Value1" },
            { "Header2", "Value2" }
        });

        // Act
        HttpRequestMessageExtensions.CopyHeadersFrom(requestMessage, httpRequest);

        // Assert
        Assert.Equal("Value1", requestMessage.Headers.GetValues("Header1").First());
        Assert.Equal("Value2", requestMessage.Headers.GetValues("Header2").First());
    }

    [Fact]
    public void CopyHeadersFrom_ShouldExcludeSpecifiedHeaders()
    {
        // Arrange
        HttpRequestMessage requestMessage = new();
        HttpRequest httpRequest = Substitute.For<HttpRequest>();
        httpRequest.Headers.Returns(new HeaderDictionary
        {
            { "Header1", "Value1" },
            { "Header2", "Value2" },
            { "Header3", "Value3" }
        });

        string[] exceptHeaders = ["Header2"];

        // Act
        HttpRequestMessageExtensions.CopyHeadersFrom(requestMessage, httpRequest, exceptHeaders);

        // Assert
        Assert.Equal("Value1", requestMessage.Headers.GetValues("Header1").First());
        Assert.False(requestMessage.Headers.Contains("Header2"));
        Assert.Equal("Value3", requestMessage.Headers.GetValues("Header3").First());
    }

    [Fact]
    public void CopyHeadersFrom_ShouldBeCaseInsensitiveForExclusions()
    {
        // Arrange
        HttpRequestMessage requestMessage = new();
        HttpRequest httpRequest = Substitute.For<HttpRequest>();
        httpRequest.Headers.Returns(new HeaderDictionary
        {
            { "Header1", "Value1" },
            { "Header2", "Value2" }
        });

        string[] exceptHeaders = ["header1"]; // Different case than in the headers

        // Act
        HttpRequestMessageExtensions.CopyHeadersFrom(requestMessage, httpRequest, exceptHeaders);

        // Assert
        Assert.False(requestMessage.Headers.Contains("Header1"));
        Assert.Equal("Value2", requestMessage.Headers.GetValues("Header2").First());
    }

    [Fact]
    public void CopyHeadersFrom_ShouldHandleEmptyHeadersGracefully()
    {
        // Arrange
        HttpRequestMessage requestMessage = new();
        HttpRequest httpRequest = Substitute.For<HttpRequest>();
        httpRequest.Headers.Returns(new HeaderDictionary());

        // Act
        HttpRequestMessageExtensions.CopyHeadersFrom(requestMessage, httpRequest);

        // Assert
        Assert.Empty(requestMessage.Headers);
    }

    [Fact]
    public void CopyHeadersFrom_ShouldHandleNullExceptHeaders()
    {
        // Arrange
        HttpRequestMessage requestMessage = new();
        HttpRequest httpRequest = Substitute.For<HttpRequest>();
        httpRequest.Headers.Returns(new HeaderDictionary
        {
            { "Header1", "Value1" }
        });

        // Act
        HttpRequestMessageExtensions.CopyHeadersFrom(requestMessage, httpRequest, null);

        // Assert
        Assert.Equal("Value1", requestMessage.Headers.GetValues("Header1").First());
    }


    [Fact]
    public async Task CopyContentFrom_ShouldCopyRequestBodyToRequestMessageContent()
    {
        // Arrange
        HttpRequestMessage requestMessage = new();
        HttpRequest httpRequest = Substitute.For<HttpRequest>();

        string sampleContent = "Test body content";
        MemoryStream bodyStream = new();
        StreamWriter writer = new(bodyStream);
        writer.Write(sampleContent);
        writer.Flush();
        bodyStream.Seek(0, SeekOrigin.Begin); // Reset stream position

        httpRequest.Body.Returns(bodyStream);

        // Act
        requestMessage.CopyContentFrom(httpRequest);

        // Assert
        Assert.NotNull(requestMessage.Content);
        string content = await requestMessage.Content.ReadAsStringAsync();
        Assert.Equal(sampleContent, content);
    }

    [Fact]
    public void CopyContentFrom_ShouldSetContentTypeHeaderIfPresent()
    {
        // Arrange
        HttpRequestMessage requestMessage = new();
        HttpRequest httpRequest = Substitute.For<HttpRequest>();

        httpRequest.Body.Returns(new MemoryStream());
        httpRequest.Headers.Returns(new HeaderDictionary
        {
            { "Content-Type", "application/json" }
        });

        // Act
        requestMessage.CopyContentFrom(httpRequest);

        // Assert
        Assert.NotNull(requestMessage.Content);
        Assert.Equal("application/json", requestMessage.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public void CopyContentFrom_ShouldNotSetContentTypeHeaderIfNotPresent()
    {
        // Arrange
        HttpRequestMessage requestMessage = new();
        HttpRequest httpRequest = Substitute.For<HttpRequest>();

        httpRequest.Body.Returns(new MemoryStream());
        httpRequest.Headers.Returns(new HeaderDictionary());

        // Act
        requestMessage.CopyContentFrom(httpRequest);

        // Assert
        Assert.NotNull(requestMessage.Content);
        Assert.Null(requestMessage.Content.Headers.ContentType);
    }

    [Fact]
    public void CopyContentFrom_ShouldHandleNullBodyGracefully()
    {
        // Arrange
        HttpRequestMessage requestMessage = new();
        HttpRequest httpRequest = Substitute.For<HttpRequest>();

        // Set the body to null
        httpRequest.Body.Returns((Stream)null!);

        // Act
        Exception exception = Record.Exception(() => requestMessage.CopyContentFrom(httpRequest));

        // Assert
        Assert.Null(exception); // Method should handle null gracefully
        Assert.Null(requestMessage.Content); // Content should remain null
    }
}
