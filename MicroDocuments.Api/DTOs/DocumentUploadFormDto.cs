using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MicroDocuments.Domain.Enums;

namespace MicroDocuments.Api.DTOs;

public class DocumentUploadFormDto
{
    [Required]
    public IFormFile File { get; set; } = null!;

    public string? Filename { get; set; }

    public string? ContentType { get; set; }

    [Required]
    public DocumentType DocumentType { get; set; }

    [Required]
    public Channel Channel { get; set; }

    public string? CustomerId { get; set; }

    public string? CorrelationId { get; set; }
}

