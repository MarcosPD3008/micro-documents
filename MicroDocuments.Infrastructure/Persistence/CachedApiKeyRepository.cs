using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Ports;

namespace MicroDocuments.Infrastructure.Persistence;

public class CachedApiKeyRepository : IApiKeyRepository
{
    private readonly IApiKeyRepository _repository;
    private readonly IApiKeyCacheService _cacheService;

    public CachedApiKeyRepository(
        IApiKeyRepository repository,
        IApiKeyCacheService cacheService)
    {
        _repository = repository;
        _cacheService = cacheService;
    }

    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        // Try cache first
        var cached = _cacheService.GetByKeyHash(keyHash);
        if (cached != null)
        {
            return cached;
        }

        // Fallback to repository if not in cache
        var apiKey = await _repository.GetByKeyHashAsync(keyHash, cancellationToken);
        if (apiKey != null)
        {
            _cacheService.AddOrUpdate(apiKey);
        }

        return apiKey;
    }

    public async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<ApiKey>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllActiveAsync(cancellationToken);
    }

    public async Task<ApiKey> CreateAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        var saved = await _repository.CreateAsync(apiKey, cancellationToken);
        _cacheService.AddOrUpdate(saved);
        return saved;
    }

    public async Task<ApiKey> UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        var oldKeyHash = await GetOldKeyHashAsync(apiKey.Id, cancellationToken);
        var saved = await _repository.UpdateAsync(apiKey, cancellationToken);

        if (oldKeyHash != null && oldKeyHash != saved.KeyHash)
        {
            _cacheService.InvalidateByKeyHash(oldKeyHash);
        }

        _cacheService.AddOrUpdate(saved);
        return saved;
    }

    private async Task<string?> GetOldKeyHashAsync(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        return existing?.KeyHash;
    }
}

