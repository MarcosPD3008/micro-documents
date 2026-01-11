using MicroDocuments.Domain.Entities;

namespace MicroDocuments.Domain.Ports;

public interface IApiKeyCacheService
{
    Task InitializeAsync(IApiKeyRepository repository, CancellationToken cancellationToken = default);
    ApiKey? GetByKeyHash(string keyHash);
    void InvalidateByKeyHash(string keyHash);
    void InvalidateById(Guid id);
    void AddOrUpdate(ApiKey apiKey);
    void Remove(string keyHash);
    bool HasAnyApiKeys();
}

