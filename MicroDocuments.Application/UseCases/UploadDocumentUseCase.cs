using Microsoft.Extensions.Logging;
using MicroDocuments.Application.DTOs;
using MicroDocuments.Application.Mappings;
using MicroDocuments.Domain.Ports;

namespace MicroDocuments.Application.UseCases;

public class UploadDocumentUseCase
{
    private readonly IDocumentRepository _repository;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<UploadDocumentUseCase> _logger;

    public UploadDocumentUseCase(
        IDocumentRepository repository,
        IFileStorage fileStorage,
        ILogger<UploadDocumentUseCase> logger)
    {
        _repository = repository;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public virtual async Task<DocumentUploadResponseDto> ExecuteAsync(
        DocumentUploadRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var fileContent = Convert.FromBase64String(request.EncodedFile);
        var fileSize = fileContent.Length;

        var documentId = Guid.NewGuid();
        var document = request.ToEntity(
            documentId,
            DateTime.UtcNow,
            fileSize);

        try
        {
            await _fileStorage.SaveAsync(documentId, fileContent, cancellationToken);
            document = await _repository.SaveAsync(document, cancellationToken);

            _logger.LogInformation(
                "UploadDocumentUseCase.ExecuteAsync - Document uploaded, DocumentId: {DocumentId}, Filename: {Filename}, CreatedBy: {CreatedBy}",
                document.Id, document.Filename, document.CreatedBy);

            return document.ToUploadResponseDto();
        }
        catch (Exception ex)
        {
            // Clean up temporary file if upload fails
            try
            {
                await _fileStorage.DeleteAsync(documentId, cancellationToken);
                _logger.LogWarning(
                    "UploadDocumentUseCase.ExecuteAsync - Cleaned up temporary file after upload failure, DocumentId: {DocumentId}",
                    documentId);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx,
                    "UploadDocumentUseCase.ExecuteAsync - Failed to cleanup temporary file, DocumentId: {DocumentId}",
                    documentId);
            }

            throw;
        }
    }

    public virtual async Task<DocumentUploadResponseDto> ExecuteStreamAsync(
        DocumentUploadRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.FileStream == null)
        {
            throw new ArgumentException("FileStream is required for streaming upload");
        }

        var documentId = Guid.NewGuid();
        var fileSize = request.FileSize ?? 0;

        var document = request.ToEntity(
            documentId,
            DateTime.UtcNow,
            fileSize);

        try
        {
            // Save using streaming
            await _fileStorage.SaveFromStreamAsync(documentId, request.FileStream, cancellationToken);
            document = await _repository.SaveAsync(document, cancellationToken);

            _logger.LogInformation(
                "UploadDocumentUseCase.ExecuteStreamAsync - Document uploaded via stream, DocumentId: {DocumentId}, Filename: {Filename}, CreatedBy: {CreatedBy}",
                document.Id, document.Filename, document.CreatedBy);

            return document.ToUploadResponseDto();
        }
        catch (Exception ex)
        {
            // Clean up temporary file if upload fails
            try
            {
                await _fileStorage.DeleteAsync(documentId, cancellationToken);
                _logger.LogWarning(
                    "UploadDocumentUseCase.ExecuteStreamAsync - Cleaned up temporary file after upload failure, DocumentId: {DocumentId}",
                    documentId);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx,
                    "UploadDocumentUseCase.ExecuteStreamAsync - Failed to cleanup temporary file, DocumentId: {DocumentId}",
                    documentId);
            }

            throw;
        }
    }
}

