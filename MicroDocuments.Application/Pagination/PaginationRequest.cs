using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MicroDocuments.Application.Pagination;

public class PaginationRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Filter { get; set; }

    [DefaultValue("Created")]
    public string SortBy { get; set; } = "Created";

    [DefaultValue("DESC")]
    public string SortDirection { get; set; } = "DESC";
}

