using System.ComponentModel.DataAnnotations;
using MicroDocuments.Domain.Enums;

namespace MicroDocuments.Application.DTOs;

public class SearchDocumentsDto
{
    public DateTime? UploadDateStart { get; set; }
    public DateTime? UploadDateEnd { get; set; }
    public string? Filename { get; set; }
public string? ContentType { get; set; }
    public DocumentType? DocumentType { get; set; }
    public DocumentStatus? Status { get; set; }
    public string? CustomerId { get; set; }
    public Channel? Channel { get; set; }

    [Required]
    public string SortBy { get; set; } = string.Empty;

    public string SortDirection { get; set; } = "ASC";
}

