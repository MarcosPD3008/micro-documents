using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Ports;
using MicroDocuments.Infrastructure.Persistence;

namespace MicroDocuments.Infrastructure.Persistence;

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiKeyRepository(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    private Guid? GetCurrentApiKeyId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue("ApiKey", out var apiKey) == true && apiKey is Domain.Entities.ApiKey key)
        {
            return key.Id;
        }
        return null;
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

    public async Task<ApiKey> SaveAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        var currentApiKeyId = GetCurrentApiKeyId();
        
        if (apiKey.Id == Guid.Empty)
        {
            apiKey.Id = Guid.NewGuid();
            apiKey.Created = DateTime.UtcNow;
            apiKey.CreatedBy = currentApiKeyId;
            _context.ApiKeys.Add(apiKey);
        }
        else
        {
            var existing = await _context.ApiKeys.FindAsync(new object[] { apiKey.Id }, cancellationToken);
            if (existing == null)
            {
                apiKey.Created = DateTime.UtcNow;
                apiKey.CreatedBy = currentApiKeyId;
                _context.ApiKeys.Add(apiKey);
            }
            else
            {
                if (apiKey.Deleted.HasValue && !existing.Deleted.HasValue)
                {
                    apiKey.DeletedBy = currentApiKeyId;
                }
                else if (!apiKey.Deleted.HasValue)
                {
                    apiKey.Updated = DateTime.UtcNow;
                    apiKey.UpdatedBy = currentApiKeyId;
                }
                _context.Entry(existing).CurrentValues.SetValues(apiKey);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return apiKey;
    }
}

