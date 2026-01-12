using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Enums;
using MicroDocuments.Domain.Ports;

namespace MicroDocuments.Infrastructure.BackgroundJobs;

public class DocumentUploadBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentUploadBackgroundService> _logger;
    private int _cleanupCycleCounter = 0;
    private const int CLEANUP_INTERVAL_CYCLES = 60;

    public DocumentUploadBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DocumentUploadBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingUploads(stoppingToken);

                _cleanupCycleCounter++;
                if (_cleanupCycleCounter >= CLEANUP_INTERVAL_CYCLES)
                {
                    await CleanupOrphanedFilesAsync(stoppingToken);
                    _cleanupCycleCounter = 0;
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentUploadBackgroundService.ExecuteAsync - Error processing uploads");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task ProcessPendingUploads(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IDocumentPublisher>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        var pendingDocuments = repository.GetAll()
            .Where(d => d.Status == DocumentStatus.RECEIVED)
            .Take(10)
            .ToList();

        foreach (var document in pendingDocuments)
        {
            try
            {
                await ProcessDocumentAsync(document, repository, publisher, fileStorage, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "DocumentUploadBackgroundService.ProcessPendingUploads - Error processing document {DocumentId}",
                    document.Id);

                try
                {
                    await fileStorage.DeleteAsync(document.Id, cancellationToken);
                    _logger.LogWarning(
                        "DocumentUploadBackgroundService.ProcessPendingUploads - Cleaned up temporary file after processing failure, DocumentId: {DocumentId}",
                        document.Id);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx,
                        "DocumentUploadBackgroundService.ProcessPendingUploads - Failed to cleanup temporary file, DocumentId: {DocumentId}",
                        document.Id);
                }

                document.Status = DocumentStatus.FAILED;
                document.Updated = DateTime.UtcNow;
                await repository.SaveAsync(document, cancellationToken);
            }
        }
    }

    private async Task ProcessDocumentAsync(
        Document document,
        IDocumentRepository repository,
        IDocumentPublisher publisher,
        IFileStorage fileStorage,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DocumentUploadBackgroundService.ProcessDocumentAsync - Processing document {DocumentId}, Filename: {Filename}",
            document.Id, document.Filename);

        string url;
        
        await using (var fileStream = await fileStorage.GetStreamAsync(document.Id, cancellationToken))
        {
            url = await publisher.PublishStreamAsync(document, fileStream, cancellationToken);
        }

        document.Status = DocumentStatus.SENT;
        document.Url = url;
        document.Updated = DateTime.UtcNow;

        await repository.SaveAsync(document, cancellationToken);
        
        _logger.LogInformation(
            "DocumentUploadBackgroundService.ProcessDocumentAsync - Document status updated, DocumentId: {DocumentId}, UpdatedBy: {UpdatedBy}",
            document.Id, document.UpdatedBy);
        
        try
        {
            await fileStorage.DeleteAsync(document.Id, cancellationToken);
            _logger.LogInformation(
                "DocumentUploadBackgroundService.ProcessDocumentAsync - Temporary file deleted, DocumentId: {DocumentId}",
                document.Id);
        }
        catch (Exception deleteEx)
        {
            _logger.LogWarning(deleteEx,
                "DocumentUploadBackgroundService.ProcessDocumentAsync - Failed to delete temporary file (document already sent), DocumentId: {DocumentId}",
                document.Id);
        }

        _logger.LogInformation(
            "DocumentUploadBackgroundService.ProcessDocumentAsync - Document processed successfully {DocumentId}, Url: {Url}",
            document.Id, url);
    }

    private async Task CleanupOrphanedFilesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        try
        {
            var allDocumentIds = repository.GetAll()
                .Select(d => d.Id)
                .ToHashSet();

            await fileStorage.CleanupOrphanedFilesAsync(allDocumentIds, cancellationToken);

            _logger.LogInformation(
                "DocumentUploadBackgroundService.CleanupOrphanedFilesAsync - Orphaned files cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "DocumentUploadBackgroundService.CleanupOrphanedFilesAsync - Error during orphaned files cleanup");
        }
    }
}

