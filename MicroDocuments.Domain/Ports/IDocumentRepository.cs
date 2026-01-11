using MicroDocuments.Domain.Entities;

namespace MicroDocuments.Domain.Ports;

public interface IDocumentRepository
{
    Task<Document> SaveAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    IQueryable<Document> GetAll();
}

