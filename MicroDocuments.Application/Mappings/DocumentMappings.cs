using System.Linq;
using MicroDocuments.Application.DTOs;
using MicroDocuments.Domain.Entities;

namespace MicroDocuments.Application.Mappings;

public static class DocumentMappings
{
    public static DocumentUploadResponseDto ToUploadResponseDto(this Document document)
    {
        return new DocumentUploadResponseDto
        {
            Id = document.Id.ToString()
        };
    }

    public static DocumentAssetDto ToAssetDto(this Document document)
    {
        return new DocumentAssetDto
        {
            Id = document.Id.ToString(),
            Filename = document.Filename,
            ContentType = document.ContentType,
            DocumentType = document.DocumentType,
            Channel = document.Channel,
            CustomerId = document.CustomerId,
            Status = document.Status,
            Url = document.Url,
            Size = document.Size,
            UploadDate = document.UploadDate,
            CorrelationId = document.CorrelationId
        };
    }

    public static IQueryable<DocumentAssetDto> ToAssetDtoQuery(this IQueryable<Document> query)
    {
        return query.Select(d => new DocumentAssetDto
        {
            Id = d.Id.ToString(),
            Filename = d.Filename,
            ContentType = d.ContentType,
            DocumentType = d.DocumentType,
            Channel = d.Channel,
            CustomerId = d.CustomerId,
            Status = d.Status,
            Url = d.Url,
            Size = d.Size,
            UploadDate = d.UploadDate,
            CorrelationId = d.CorrelationId
        });
    }

    public static Document ToEntity(this DocumentUploadRequestDto dto, Guid id, DateTime uploadDate, long? fileSize = null)
    {
        // Use FileSize from DTO if provided, otherwise use the passed fileSize parameter
        var finalFileSize = dto.FileSize ?? fileSize ?? 0;
        
        return new Document
        {
            Id = id,
            Filename = dto.Filename,
            ContentType = dto.ContentType,
            DocumentType = dto.DocumentType,
            Channel = dto.Channel,
            CustomerId = dto.CustomerId,
            CorrelationId = dto.CorrelationId,
            Status = Domain.Enums.DocumentStatus.RECEIVED,
            Size = finalFileSize,
            UploadDate = uploadDate,
            Created = DateTime.UtcNow
        };
    }
}

