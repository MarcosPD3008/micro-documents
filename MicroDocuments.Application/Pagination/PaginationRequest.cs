using System.ComponentModel.DataAnnotations;

namespace MicroDocuments.Application.Pagination;

public class PaginationRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Filter { get; set; }

    [Required]
    public string SortBy { get; set; } = string.Empty;

    public string SortDirection { get; set; } = "ASC";
}

