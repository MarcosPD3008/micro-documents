using MicroDocuments.Domain.Entities;

namespace MicroDocuments.Domain.Ports;

public interface IDocumentPublisher
{
    Task<string> PublishAsync(Document document, byte[] fileContent, CancellationToken cancellationToken = default);
    Task<string> PublishStreamAsync(Document document, Stream fileStream, CancellationToken cancellationToken = default);
}

