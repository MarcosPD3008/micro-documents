using MicroDocuments.Domain.Entities;

namespace MicroDocuments.Domain.Ports;

public interface IDocumentRepository
{
    Task<Document> CreateAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document> UpdateAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    IQueryable<Document> GetAll();
}

