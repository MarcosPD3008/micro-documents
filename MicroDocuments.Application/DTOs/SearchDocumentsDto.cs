using System.ComponentModel;
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

    [DefaultValue("Created")]
    public string SortBy { get; set; } = "Created";

    [DefaultValue("DESC")]
    public string SortDirection { get; set; } = "DESC";
}

