using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Infrastructure.Configuration;

namespace MicroDocuments.Infrastructure.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimiterSettings _settings;
    private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IOptions<ResilienceSettings> resilienceSettings)
    {
        _next = next;
        _settings = resilienceSettings.Value.RateLimiter;
        _rateLimitStore = new ConcurrentDictionary<string, RateLimitInfo>();
        _semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_settings.Enabled)
        {
            await _next(context);
            return;
        }

        // Get API key from context (set by ApiKeyAuthenticationMiddleware)
        if (!context.Items.TryGetValue("ApiKey", out var apiKeyObj) || apiKeyObj is not ApiKey apiKey)
        {
            // No API key in context, skip rate limiting (shouldn't happen if auth middleware is before this)
            await _next(context);
            return;
        }

        var apiKeyId = apiKey.Id.ToString();
        var now = DateTime.UtcNow;

        var semaphore = _semaphores.GetOrAdd(apiKeyId, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync();
        try
        {
            var rateLimitInfo = _rateLimitStore.AddOrUpdate(
                apiKeyId,
                new RateLimitInfo { WindowStart = now, RequestCount = 1 },
                (key, existing) =>
                {
                    var elapsed = now - existing.WindowStart;

                    // Reset window if a minute has passed
                    if (elapsed >= TimeSpan.FromMinutes(1))
                    {
                        return new RateLimitInfo { WindowStart = now, RequestCount = 1 };
                    }

                    // Increment count
                    return new RateLimitInfo
                    {
                        WindowStart = existing.WindowStart,
                        RequestCount = existing.RequestCount + 1
                    };
                });

            if (rateLimitInfo.RequestCount > apiKey.RateLimitPerMinute)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }
        }
        finally
        {
            semaphore.Release();
        }

        await _next(context);
    }

    private class RateLimitInfo
    {
        public DateTime WindowStart { get; set; }
        public int RequestCount { get; set; }
    }
}
