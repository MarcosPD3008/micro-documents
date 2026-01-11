using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Ports;
using MicroDocuments.Infrastructure.Configuration;

namespace MicroDocuments.Infrastructure.Persistence;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApiKeySettings _apiKeySettings;

    public DocumentRepository(
        AppDbContext context, 
        IHttpContextAccessor httpContextAccessor,
        IOptions<ApiKeySettings> apiKeySettings)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _apiKeySettings = apiKeySettings.Value;
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

    public async Task<Document> SaveAsync(Document document, CancellationToken cancellationToken = default)
    {
        var apiKeyId = GetCurrentApiKeyId();
        
        if (!string.IsNullOrEmpty(document.Id.ToString()) && document.Id == Guid.Empty)
        {
            document.Id = Guid.NewGuid();
            document.Created = DateTime.UtcNow;
            document.CreatedBy = apiKeyId;
            _context.Documents.Add(document);
        }
        else
        {
            var existing = await _context.Documents.FindAsync(document.Id, cancellationToken);
            if (existing == null)
            {
                document.Created = DateTime.UtcNow;
                document.CreatedBy = apiKeyId;
                _context.Documents.Add(document);
            }
            else
            {
                document.Updated = DateTime.UtcNow;
                document.UpdatedBy = apiKeyId;
                _context.Entry(existing).CurrentValues.SetValues(document);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Documents.AsQueryable();
        
        if (_apiKeySettings.GlobalFilter)
        {
            var apiKeyId = GetCurrentApiKeyId();
            if (apiKeyId.HasValue)
            {
                query = query.Where(d => d.Id == id && d.CreatedBy == apiKeyId);
            }
            else
            {
                return null;
            }
        }
        else
        {
            query = query.Where(d => d.Id == id);
        }
        
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public IQueryable<Document> GetAll()
    {
        var query = _context.Documents.AsQueryable();
        
        if (_apiKeySettings.GlobalFilter)
        {
            var apiKeyId = GetCurrentApiKeyId();
            if (apiKeyId.HasValue)
            {
                query = query.Where(d => d.CreatedBy == apiKeyId);
            }
            else
            {
                query = query.Where(d => false); // Return empty query if no API key
            }
        }
        
        return query;
    }
}

