using Microsoft.Extensions.Logging;
using MicroDocuments.Domain.Ports;

namespace MicroDocuments.Infrastructure.ExternalServices;

public class LocalFileStorage : IFileStorage
{
    private readonly string _storagePath;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(ILogger<LocalFileStorage> logger)
    {
        _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "temp_uploads");
        _logger = logger;

        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task SaveAsync(Guid documentId, byte[] content, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(documentId);
        await File.WriteAllBytesAsync(filePath, content, cancellationToken);
        
        _logger.LogInformation(
            "LocalFileStorage.SaveAsync - File saved, DocumentId: {DocumentId}, Path: {Path}",
            documentId, filePath);
    }

    public async Task<byte[]> GetAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(documentId);
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found for document {documentId}");
        }

        return await File.ReadAllBytesAsync(filePath, cancellationToken);
    }

    public async Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(documentId);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation(
                "LocalFileStorage.DeleteAsync - File deleted, DocumentId: {DocumentId}",
                documentId);
        }

        await Task.CompletedTask;
    }

    public async Task SaveFromStreamAsync(Guid documentId, Stream sourceStream, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(documentId);
        
        await using var fileStream = new FileStream(
            filePath, 
            FileMode.Create, 
            FileAccess.Write, 
            FileShare.None, 
            bufferSize: 81920, 
            useAsync: true);
        
        await sourceStream.CopyToAsync(fileStream, cancellationToken);
        
        _logger.LogInformation(
            "LocalFileStorage.SaveFromStreamAsync - File saved via stream, DocumentId: {DocumentId}, Path: {Path}",
            documentId, filePath);
    }

    public async Task<Stream> GetStreamAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(documentId);
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found for document {documentId}");
        }
        
        return new FileStream(
            filePath, 
            FileMode.Open, 
            FileAccess.Read, 
            FileShare.Read, 
            bufferSize: 81920, 
            useAsync: true);
    }

    public IEnumerable<Guid> GetAllTempFileIds()
    {
        if (!Directory.Exists(_storagePath))
        {
            return Enumerable.Empty<Guid>();
        }

        return Directory.GetFiles(_storagePath, "*.tmp")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name) && Guid.TryParse(name, out _))
            .Select(name => Guid.Parse(name!));
    }

    public async Task CleanupOrphanedFilesAsync(IEnumerable<Guid> validDocumentIds, CancellationToken cancellationToken = default)
    {
        var tempFileIds = GetAllTempFileIds();
        var orphanedIds = tempFileIds.Except(validDocumentIds);

        foreach (var orphanedId in orphanedIds)
        {
            try
            {
                await DeleteAsync(orphanedId, cancellationToken);
                _logger.LogWarning(
                    "LocalFileStorage.CleanupOrphanedFilesAsync - Cleaned up orphaned temporary file, DocumentId: {DocumentId}",
                    orphanedId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "LocalFileStorage.CleanupOrphanedFilesAsync - Failed to cleanup orphaned file, DocumentId: {DocumentId}",
                    orphanedId);
            }
        }
    }

    private string GetFilePath(Guid documentId)
    {
        return Path.Combine(_storagePath, $"{documentId}.tmp");
    }
}

