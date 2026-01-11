using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Ports;
using MicroDocuments.Infrastructure.Middleware;
using MicroDocuments.Infrastructure.Services;
using Moq;
using Xunit;

namespace MicroDocuments.Tests.Infrastructure.Middleware;

public class ApiKeyAuthenticationMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<IApiKeyRepository> _apiKeyRepositoryMock;
    private readonly Mock<IApiKeyCacheService> _cacheServiceMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly ApiKeyService _apiKeyService;
    private readonly Mock<ILogger<ApiKeyAuthenticationMiddleware>> _loggerMock;
    private readonly ApiKeyAuthenticationMiddleware _middleware;

    public ApiKeyAuthenticationMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _apiKeyRepositoryMock = new Mock<IApiKeyRepository>();
        _cacheServiceMock = new Mock<IApiKeyCacheService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        
        // Create real ApiKeyService with mock configuration
        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x["ApiKey:SecretKey"]).Returns("test-secret-key-for-hmac");
        _apiKeyService = new ApiKeyService(configurationMock.Object);
        
        _loggerMock = new Mock<ILogger<ApiKeyAuthenticationMiddleware>>();
        
        // Setup service provider to return repository when creating scope
        var serviceScopeMock = new Mock<IServiceScope>();
        var scopedServiceProviderMock = new Mock<IServiceProvider>();
        
        // Mock GetService instead of GetRequiredService (extension method)
        scopedServiceProviderMock.Setup(x => x.GetService(typeof(IApiKeyRepository)))
            .Returns(_apiKeyRepositoryMock.Object);
        serviceScopeMock.Setup(x => x.ServiceProvider).Returns(scopedServiceProviderMock.Object);
        
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);
        
        // Mock GetService for IServiceScopeFactory
        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactoryMock.Object);
        
        _middleware = new ApiKeyAuthenticationMiddleware(
            _nextMock.Object,
            _serviceProviderMock.Object,
            _apiKeyService,
            _cacheServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_Should_PassThrough_When_HealthCheckEndpoint()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_Should_PassThrough_When_SwaggerEndpoint()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/swagger/index.html";
        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_Should_PassThrough_When_SwaggerJsonEndpoint()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/swagger/v1/swagger.json";
        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_Should_Return401_When_ApiKeyHeaderMissing()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Should_Return401_When_ApiKeyInvalid()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Headers["X-API-Key"] = "invalid-key";
        
        var keyHash = _apiKeyService.HashApiKey("invalid-key");
        _cacheServiceMock.Setup(x => x.GetByKeyHash(keyHash))
            .Returns((ApiKey?)null);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Should_PassThrough_When_ApiKeyValid()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        var validApiKeyValue = "valid-key";
        context.Request.Headers["X-API-Key"] = validApiKeyValue;
        
        var keyHash = _apiKeyService.HashApiKey(validApiKeyValue);
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = "Test Key",
            KeyHash = keyHash,
            IsActive = true,
            RateLimitPerMinute = 100
        };

        _cacheServiceMock.Setup(x => x.GetByKeyHash(keyHash))
            .Returns(apiKey);
        
        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        context.Items["ApiKey"].Should().Be(apiKey);
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_UseCache_When_ApiKeyCached()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        var cachedApiKeyValue = "cached-key";
        context.Request.Headers["X-API-Key"] = cachedApiKeyValue;
        
        var keyHash = _apiKeyService.HashApiKey(cachedApiKeyValue);
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = "Cached Key",
            KeyHash = keyHash,
            IsActive = true,
            RateLimitPerMinute = 100
        };
        
        // Cache the API key
        _cacheServiceMock.Setup(x => x.GetByKeyHash(keyHash))
            .Returns(apiKey);
        
        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        context.Items["ApiKey"].Should().Be(apiKey);
        // Should use cache service
        _cacheServiceMock.Verify(x => x.GetByKeyHash(keyHash), Times.Once);
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Return401_When_ApiKeyInactive()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Headers["X-API-Key"] = "inactive-key";
        
        var keyHash = _apiKeyService.HashApiKey("inactive-key");
        _cacheServiceMock.Setup(x => x.GetByKeyHash(keyHash))
            .Returns((ApiKey?)null); // Cache service filters inactive keys

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Should_Return401_When_ApiKeyExpired()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Headers["X-API-Key"] = "expired-key";
        
        var keyHash = _apiKeyService.HashApiKey("expired-key");
        _cacheServiceMock.Setup(x => x.GetByKeyHash(keyHash))
            .Returns((ApiKey?)null); // Cache service filters expired keys

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }
}

