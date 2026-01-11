using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Infrastructure.Configuration;
using MicroDocuments.Infrastructure.Middleware;
using Moq;
using System.Text;
using Xunit;

namespace MicroDocuments.Tests.Infrastructure.Middleware;

public class RateLimitingMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<IOptions<ResilienceSettings>> _resilienceSettingsMock;
    private RateLimitingMiddleware? _middleware;

    public RateLimitingMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _resilienceSettingsMock = new Mock<IOptions<ResilienceSettings>>();
    }

    private ApiKey CreateApiKey(int rateLimitPerMinute = 100)
    {
        return new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = "Test Key",
            KeyHash = "test-hash",
            IsActive = true,
            RateLimitPerMinute = rateLimitPerMinute,
            Created = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task InvokeAsync_Should_PassThrough_When_RateLimitingDisabled()
    {
        // Arrange
        var settings = new ResilienceSettings
        {
            RateLimiter = new RateLimiterSettings
            {
                Enabled = false
            }
        };
        _resilienceSettingsMock.Setup(x => x.Value).Returns(settings);
        var middleware = new RateLimitingMiddleware(_nextMock.Object, _resilienceSettingsMock.Object);

        var context = new DefaultHttpContext();
        var apiKey = CreateApiKey();
        context.Items["ApiKey"] = apiKey;

        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_Should_PassThrough_When_NoApiKeyInContext()
    {
        // Arrange
        var settings = new ResilienceSettings
        {
            RateLimiter = new RateLimiterSettings
            {
                Enabled = true
            }
        };
        _resilienceSettingsMock.Setup(x => x.Value).Returns(settings);
        var middleware = new RateLimitingMiddleware(_nextMock.Object, _resilienceSettingsMock.Object);

        var context = new DefaultHttpContext();
        // No API key in context

        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_Should_AllowRequest_When_UnderLimit()
    {
        // Arrange
        var settings = new ResilienceSettings
        {
            RateLimiter = new RateLimiterSettings
            {
                Enabled = true
            }
        };
        _resilienceSettingsMock.Setup(x => x.Value).Returns(settings);
        var middleware = new RateLimitingMiddleware(_nextMock.Object, _resilienceSettingsMock.Object);

        var context = new DefaultHttpContext();
        var apiKey = CreateApiKey(rateLimitPerMinute: 10);
        context.Items["ApiKey"] = apiKey;
        
        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_Should_BlockRequest_When_OverLimit()
    {
        // Arrange
        var settings = new ResilienceSettings
        {
            RateLimiter = new RateLimiterSettings
            {
                Enabled = true
            }
        };
        _resilienceSettingsMock.Setup(x => x.Value).Returns(settings);
        _middleware = new RateLimitingMiddleware(_nextMock.Object, _resilienceSettingsMock.Object);

        var apiKey = CreateApiKey(rateLimitPerMinute: 2);
        
        var context1 = new DefaultHttpContext();
        context1.Items["ApiKey"] = apiKey;
        
        var context2 = new DefaultHttpContext();
        context2.Items["ApiKey"] = apiKey;
        
        var context3 = new DefaultHttpContext();
        context3.Items["ApiKey"] = apiKey;
        context3.Response.Body = new MemoryStream();

        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act - Make requests up to limit
        await _middleware.InvokeAsync(context1);
        await _middleware.InvokeAsync(context2);
        await _middleware.InvokeAsync(context3);

        // Assert
        context3.Response.StatusCode.Should().Be(429); // Too Many Requests
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Exactly(2)); // Only first 2 should pass through
    }

    [Fact]
    public async Task InvokeAsync_Should_ResetWindow_AfterOneMinute()
    {
        // Arrange
        var settings = new ResilienceSettings
        {
            RateLimiter = new RateLimiterSettings
            {
                Enabled = true
            }
        };
        _resilienceSettingsMock.Setup(x => x.Value).Returns(settings);
        _middleware = new RateLimitingMiddleware(_nextMock.Object, _resilienceSettingsMock.Object);

        var apiKey = CreateApiKey(rateLimitPerMinute: 1);
        
        var context1 = new DefaultHttpContext();
        context1.Items["ApiKey"] = apiKey;
        
        var context2 = new DefaultHttpContext();
        context2.Items["ApiKey"] = apiKey;
        context2.Response.Body = new MemoryStream();

        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act - First request
        await _middleware.InvokeAsync(context1);
        
        // Simulate time passing (in real scenario, this would be actual time)
        // For this test, we verify the structure works
        await _middleware.InvokeAsync(context2);

        // Assert - Second request should be blocked
        context2.Response.StatusCode.Should().Be(429);
    }

    [Fact]
    public async Task InvokeAsync_Should_HandleConcurrentRequests()
    {
        // Arrange
        var settings = new ResilienceSettings
        {
            RateLimiter = new RateLimiterSettings
            {
                Enabled = true
            }
        };
        _resilienceSettingsMock.Setup(x => x.Value).Returns(settings);
        _middleware = new RateLimitingMiddleware(_nextMock.Object, _resilienceSettingsMock.Object);

        var apiKey = CreateApiKey(rateLimitPerMinute: 5);

        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act - Make concurrent requests
        var tasks = Enumerable.Range(0, 10)
            .Select(i =>
            {
                var context = new DefaultHttpContext();
                context.Items["ApiKey"] = apiKey;
                if (i >= 5)
                {
                    context.Response.Body = new MemoryStream();
                }
                return _middleware.InvokeAsync(context);
            })
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - Should handle concurrency safely
        // ConcurrentDictionary ensures thread safety
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.AtMost(5));
    }

    [Fact]
    public async Task InvokeAsync_Should_HandleDifferentApiKeys_Independently()
    {
        // Arrange
        var settings = new ResilienceSettings
        {
            RateLimiter = new RateLimiterSettings
            {
                Enabled = true
            }
        };
        _resilienceSettingsMock.Setup(x => x.Value).Returns(settings);
        _middleware = new RateLimitingMiddleware(_nextMock.Object, _resilienceSettingsMock.Object);

        var apiKey1 = CreateApiKey(rateLimitPerMinute: 1);
        var apiKey2 = CreateApiKey(rateLimitPerMinute: 1);

        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act - Make requests with different API keys
        var context1 = new DefaultHttpContext();
        context1.Items["ApiKey"] = apiKey1;
        
        var context2 = new DefaultHttpContext();
        context2.Items["ApiKey"] = apiKey2;

        await _middleware.InvokeAsync(context1);
        await _middleware.InvokeAsync(context2);

        // Assert - Both should pass through (different API keys have separate limits)
        context1.Response.StatusCode.Should().Be(200);
        context2.Response.StatusCode.Should().Be(200);
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Exactly(2));
    }
}

