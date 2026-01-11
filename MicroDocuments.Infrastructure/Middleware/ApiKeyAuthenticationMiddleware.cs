using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Ports;
using MicroDocuments.Infrastructure.Services;

namespace MicroDocuments.Infrastructure.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;
    private readonly ApiKeyService _apiKeyService;
    private readonly IApiKeyCacheService _cacheService;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
    private const string ApiKeyHeaderName = "X-API-Key";

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        IServiceProvider serviceProvider,
        ApiKeyService apiKeyService,
        IApiKeyCacheService cacheService,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _serviceProvider = serviceProvider;
        _apiKeyService = apiKeyService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health check and Swagger endpoints
        var path = context.Request.Path.Value ?? string.Empty;
        if (context.Request.Path.StartsWithSegments("/health") ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }


        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeader) ||
            string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            await WriteUnauthorizedResponse(context, "API Key is required. Please provide X-API-Key header.");
            return;
        }

        var apiKey = apiKeyHeader.ToString();
        var keyHash = _apiKeyService.HashApiKey(apiKey);

        var cachedApiKey = _cacheService.GetByKeyHash(keyHash);

        if (cachedApiKey == null)
        {
            _logger.LogWarning("ApiKeyAuthenticationMiddleware.InvokeAsync - Invalid API key attempted");
            await WriteUnauthorizedResponse(context, "Invalid API Key.");
            return;
        }

        if (!_apiKeyService.ValidateApiKey(apiKey, cachedApiKey.KeyHash))
        {
            _logger.LogWarning("ApiKeyAuthenticationMiddleware.InvokeAsync - API key validation failed");
            await WriteUnauthorizedResponse(context, "Invalid API Key.");
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();
                cachedApiKey.LastUsedAt = DateTime.UtcNow;
                await repo.SaveAsync(cachedApiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApiKeyAuthenticationMiddleware.InvokeAsync - Error updating LastUsedAt");
            }
        });

        context.Items["ApiKey"] = cachedApiKey;
        context.Items["ApiKeyId"] = cachedApiKey.Id;
        context.Items["ApiKeyName"] = cachedApiKey.Name;

        await _next(context);
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var errorResponse = new ErrorResponseDto
        {
            Error = "Unauthorized",
            Message = message,
            StatusCode = StatusCodes.Status401Unauthorized
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

