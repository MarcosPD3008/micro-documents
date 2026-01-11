using System.ComponentModel.DataAnnotations;
using MicroDocuments.Domain.Enums;

namespace MicroDocuments.Application.DTOs;

public class DocumentUploadRequestDto
{
    [Required]
    public string Filename { get; set; } = string.Empty;

    public string? EncodedFile { get; set; }

    public Stream? FileStream { get; set; }

    public long? FileSize { get; set; }

    [Required]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    public DocumentType DocumentType { get; set; }

    [Required]
    public Channel Channel { get; set; }

    public string? CustomerId { get; set; }

    public string? CorrelationId { get; set; }
}

