using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Ports;

namespace MicroDocuments.Infrastructure.Services;

public class ApiKeyCacheService : IApiKeyCacheService
{
    private readonly ILogger<ApiKeyCacheService> _logger;
    private readonly ConcurrentDictionary<string, ApiKey> _cache = new();
    private readonly object _lockObject = new();
    private bool _isInitialized = false;

    public ApiKeyCacheService(ILogger<ApiKeyCacheService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(IApiKeyRepository repository, CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        lock (_lockObject)
        {
            if (_isInitialized)
            {
                return;
            }

            _logger.LogInformation("ApiKeyCacheService.InitializeAsync - Initializing cache, loading all API keys");
        }

        try
        {
            var apiKeys = await repository.GetAllActiveAsync(cancellationToken);
            
            _cache.Clear();
            foreach (var apiKey in apiKeys)
            {
                if (IsValidApiKey(apiKey))
                {
                    _cache.TryAdd(apiKey.KeyHash, apiKey);
                }
            }

            lock (_lockObject)
            {
                _isInitialized = true;
            }

            _logger.LogInformation("ApiKeyCacheService.InitializeAsync - Cache initialized successfully with {Count} API keys", _cache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApiKeyCacheService.InitializeAsync - Error initializing cache");
            throw;
        }
    }

    public ApiKey? GetByKeyHash(string keyHash)
    {
        if (!_isInitialized)
        {
            _logger.LogWarning("ApiKeyCacheService.GetByKeyHash - Cache not initialized");
            return null;
        }

        if (_cache.TryGetValue(keyHash, out var apiKey) && IsValidApiKey(apiKey))
        {
            return apiKey;
        }

        return null;
    }

    public void InvalidateByKeyHash(string keyHash)
    {
        _cache.TryRemove(keyHash, out _);
        _logger.LogInformation("ApiKeyCacheService.InvalidateByKeyHash - Invalidated cache for key hash");
    }

    public void InvalidateById(Guid id)
    {
        var keyToRemove = _cache.Values
            .FirstOrDefault(k => k.Id == id)?.KeyHash;

        if (keyToRemove != null)
        {
            _cache.TryRemove(keyToRemove, out _);
            _logger.LogInformation("ApiKeyCacheService.InvalidateById - Invalidated cache for API key {Id}", id);
        }
    }

    public void AddOrUpdate(ApiKey apiKey)
    {
        if (IsValidApiKey(apiKey))
        {
            _cache.AddOrUpdate(apiKey.KeyHash, apiKey, (key, oldValue) => apiKey);
            _logger.LogInformation("ApiKeyCacheService.AddOrUpdate - Updated cache for API key {Id}", apiKey.Id);
        }
        else
        {
            _cache.TryRemove(apiKey.KeyHash, out _);
            _logger.LogInformation("ApiKeyCacheService.AddOrUpdate - Removed invalid API key {Id} from cache", apiKey.Id);
        }
    }

    public void Remove(string keyHash)
    {
        _cache.TryRemove(keyHash, out _);
        _logger.LogInformation("ApiKeyCacheService.Remove - Removed API key from cache");
    }

    public bool HasAnyApiKeys()
    {
        if (!_isInitialized)
        {
            return false;
        }
        return _cache.Count > 0;
    }

    private static bool IsValidApiKey(ApiKey apiKey)
    {
        return apiKey.IsActive && 
               (apiKey.ExpiresAt == null || apiKey.ExpiresAt > DateTime.UtcNow) &&
               apiKey.Deleted == null;
    }
}

