using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Infrastructure.Configuration;

namespace MicroDocuments.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ApiKeySettings? _apiKeySettings;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor, ApiKeySettings? apiKeySettings = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _apiKeySettings = apiKeySettings;
    }

    public DbSet<Document> Documents { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }

    public IQueryable<Document> GetDocuments()
    {
        var query = Documents.AsQueryable();
        
        if (_apiKeySettings?.GlobalFilter == true)
        {
            var apiKeyId = GetCurrentApiKeyId();
            if (apiKeyId.HasValue)
            {
                query = query.Where(d => d.CreatedBy == apiKeyId);
            }
            else
            {
                query = query.Where(d => false);
            }
        }
        
        return query;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private Guid? GetCurrentApiKeyId()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext?.Items.TryGetValue("ApiKey", out var apiKey) == true && apiKey is ApiKey key)
        {
            return key.Id;
        }
        return null;
    }

    public override int SaveChanges()
    {
        ApplyAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditFields()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var currentApiKeyId = GetCurrentApiKeyId();
        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            var entity = entry.Entity;

            switch (entry.State)
            {
                case EntityState.Added:
                    if (entity.Id == Guid.Empty)
                    {
                        entity.Id = Guid.NewGuid();
                    }
                    entity.Created = utcNow;
                    entity.CreatedBy = currentApiKeyId;
                    break;

                case EntityState.Modified:
                    var originalDeleted = entry.OriginalValues.GetValue<DateTime?>(nameof(BaseEntity.Deleted));
                    
                    if (entity.Deleted.HasValue && (!originalDeleted.HasValue || originalDeleted.Value == default))
                    {
                        entity.DeletedBy = currentApiKeyId ?? entity.CreatedBy;
                    }
                    else if (!entity.Deleted.HasValue)
                    {
                        entity.Updated = utcNow;
                        entity.UpdatedBy = currentApiKeyId ?? entity.CreatedBy;
                    }
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entity.Deleted = utcNow;
                    entity.DeletedBy = currentApiKeyId ?? entity.CreatedBy;
                    break;
            }
        }
    }
}

