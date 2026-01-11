using MicroDocuments.Domain.Entities;

namespace MicroDocuments.Domain.Ports;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ApiKey>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<ApiKey> SaveAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
}

