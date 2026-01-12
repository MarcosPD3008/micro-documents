using Microsoft.EntityFrameworkCore;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Ports;

namespace MicroDocuments.Infrastructure.Persistence;

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly AppDbContext _context;

    public ApiKeyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys
            .FirstOrDefaultAsync(
                k => k.KeyHash == keyHash && 
                     k.IsActive && 
                     (k.ExpiresAt == null || k.ExpiresAt > DateTime.UtcNow),
                cancellationToken);
    }

    public async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<ApiKey>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys
            .Where(k => k.IsActive && 
                       (k.ExpiresAt == null || k.ExpiresAt > DateTime.UtcNow))
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiKey> CreateAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        if (apiKey.Id == Guid.Empty)
        {
            apiKey.Id = Guid.NewGuid();
        }
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync(cancellationToken);
        return apiKey;
    }

    public async Task<ApiKey> UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ApiKeys.FindAsync(new object[] { apiKey.Id }, cancellationToken);
        if (existing == null)
        {
            throw new InvalidOperationException($"ApiKey with id {apiKey.Id} not found");
        }
        
        _context.Entry(existing).CurrentValues.SetValues(apiKey);
        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }
}

