using System.Collections.Generic;

namespace MicroDocuments.Domain.Ports;

public interface IFileStorage
{
    Task SaveAsync(Guid documentId, byte[] content, CancellationToken cancellationToken = default);
    Task<byte[]> GetAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task CleanupOrphanedFilesAsync(IEnumerable<Guid> validDocumentIds, CancellationToken cancellationToken = default);
    
    // Streaming methods
    Task SaveFromStreamAsync(Guid documentId, Stream sourceStream, CancellationToken cancellationToken = default);
    Task<Stream> GetStreamAsync(Guid documentId, CancellationToken cancellationToken = default);
}

