using System.Net;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace BenjaminAbt.WebAnalyticsProxy.UnitTests;

public class WebAnalyticsProxyFactoryTests
{
    [Fact]
    public void CreateMessageFrom_ShouldCreateHttpRequestMessage_WithCorrectUriAndMethod()
    {
        // Arrange
        HttpRequest sourceRequest = Substitute.For<HttpRequest>();
        sourceRequest.Method.Returns("GET");
        sourceRequest.QueryString.Returns(new QueryString("?param=value"));
        sourceRequest.HttpContext.Connection.RemoteIpAddress.Returns(IPAddress.Parse("127.0.0.1"));

        Uri proxyUri = new("https://example.com/collect");
        Version httpVersion = new(2, 0);

        // Act
        HttpRequestMessage result = WebAnalyticsProxyFactory.CreateMessageFrom(sourceRequest, proxyUri, httpVersion);

        // Assert
        Assert.Equal(new Uri("https://example.com/collect?param=value"), result.RequestUri);
        Assert.Equal(System.Net.Http.HttpMethod.Get, result.Method);
        Assert.Equal(httpVersion, result.Version);
    }

    [Fact]
    public void CreateMessageFrom_ShouldCopyHeaders_FromSourceRequest()
    {
        // Arrange
        HttpRequest sourceRequest = Substitute.For<HttpRequest>();
        sourceRequest.Method.Returns("GET");
        sourceRequest.QueryString.Returns(new QueryString("?param=value"));
        sourceRequest.HttpContext.Connection.RemoteIpAddress.Returns(IPAddress.Parse("127.0.0.1"));
        sourceRequest.Headers.Returns(new HeaderDictionary
            {
                { "Header1", "Value1" },
                { "Header2", "Value2" }
            });

        Uri proxyUri = new("https://example.com/collect");
        Version httpVersion = new(2, 0);

        // Act
        HttpRequestMessage result = WebAnalyticsProxyFactory.CreateMessageFrom(sourceRequest, proxyUri, httpVersion);

        // Assert
        Assert.Equal("Value1", result.Headers.GetValues("Header1").First());
        Assert.Equal("Value2", result.Headers.GetValues("Header2").First());
    }

    [Fact]
    public void CreateMessageFrom_ShouldForwardIPAddress_WhenEnabled()
    {
        // Arrange
        HttpRequest sourceRequest = Substitute.For<HttpRequest>();
        sourceRequest.Method.Returns("GET");
        sourceRequest.QueryString.Returns(new QueryString("?param=value"));
        sourceRequest.HttpContext.Connection.RemoteIpAddress.Returns(IPAddress.Parse("127.0.0.1"));

        Uri proxyUri = new("https://example.com/collect");
        Version httpVersion = new(2, 0);

        // Act
        HttpRequestMessage result = WebAnalyticsProxyFactory.CreateMessageFrom(
            sourceRequest, proxyUri, httpVersion, forwardIPAddress: true);

        // Assert
        Assert.Equal("127.0.0.1", result.Headers.GetValues("X-Forwarded-For").First());
    }

    [Fact]
    public void CreateMessageFrom_ShouldExcludeSpecifiedHeaders()
    {
        // Arrange
        HttpRequest sourceRequest = Substitute.For<HttpRequest>();
        sourceRequest.Method.Returns("GET");
        sourceRequest.QueryString.Returns(new QueryString("?param=value"));
        sourceRequest.HttpContext.Connection.RemoteIpAddress.Returns(IPAddress.Parse("127.0.0.1"));
        sourceRequest.Headers.Returns(new HeaderDictionary
            {
                { "Header1", "Value1" },
                { "Header2", "Value2" }
            });

        Uri proxyUri = new("https://example.com/collect");
        Version httpVersion = new(2, 0);
        string[] exceptHeaders = ["Header2"];

        // Act
        HttpRequestMessage result = WebAnalyticsProxyFactory.CreateMessageFrom(
            sourceRequest, proxyUri, httpVersion, exceptHeaders: exceptHeaders);

        // Assert
        Assert.Equal("Value1", result.Headers.GetValues("Header1").First());
        Assert.False(result.Headers.Contains("Header2"));
    }
}
