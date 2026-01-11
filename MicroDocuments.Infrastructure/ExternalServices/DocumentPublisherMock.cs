using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Ports;
using MicroDocuments.Infrastructure.Configuration;

namespace MicroDocuments.Infrastructure.ExternalServices;

public class DocumentPublisherMock : IDocumentPublisher
{
    private readonly DocumentPublisherSettings _settings;
    private readonly ILogger<DocumentPublisherMock> _logger;

    public DocumentPublisherMock(
        IOptions<DocumentPublisherSettings> settings,
        ILogger<DocumentPublisherMock> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> PublishAsync(Document document, byte[] fileContent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DocumentPublisherMock.PublishAsync - Simulating document upload, DocumentId: {DocumentId}, Filename: {Filename}",
            document.Id, document.Filename);

        await Task.Delay(100, cancellationToken);

        var mockUrl = $"{_settings.Url}/documents/{document.Id}";
        
        _logger.LogInformation(
            "DocumentPublisherMock.PublishAsync - Document simulated successfully, DocumentId: {DocumentId}, MockUrl: {MockUrl}",
            document.Id, mockUrl);

        return mockUrl;
    }

    public async Task<string> PublishStreamAsync(Document document, Stream fileStream, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DocumentPublisherMock.PublishStreamAsync - Simulating document upload via stream, DocumentId: {DocumentId}, Filename: {Filename}",
            document.Id, document.Filename);

        // Read stream to calculate size (for simulation purposes)
        var originalPosition = fileStream.Position;
        var streamLength = fileStream.Length;
        fileStream.Position = originalPosition;

        await Task.Delay(100, cancellationToken);

        var mockUrl = $"{_settings.Url}/documents/{document.Id}";
        
        _logger.LogInformation(
            "DocumentPublisherMock.PublishStreamAsync - Document simulated successfully via stream, DocumentId: {DocumentId}, MockUrl: {MockUrl}, Size: {Size}",
            document.Id, mockUrl, streamLength);

        return mockUrl;
    }
}

