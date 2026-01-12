using Microsoft.EntityFrameworkCore;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Ports;

namespace MicroDocuments.Infrastructure.Persistence;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _context;

    public DocumentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Document> CreateAsync(Document document, CancellationToken cancellationToken = default)
    {
        if (document.Id == Guid.Empty)
            document.Id = Guid.NewGuid();

        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }

    public async Task<Document> UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Documents.FindAsync(new object[] { document.Id }, cancellationToken) 
                     ?? throw new InvalidOperationException($"Document with id {document.Id} not found");

        _context.Entry(existing).CurrentValues.SetValues(document);
        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.GetDocuments();
        return await query.Where(d => d.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public IQueryable<Document> GetAll()
    {
        return _context.GetDocuments();
    }
}

