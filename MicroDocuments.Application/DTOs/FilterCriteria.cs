using MicroDocuments.Application.Filtering.Enums;

namespace MicroDocuments.Application.DTOs;

public class FilterCriteria
{
    public string Property { get; set; } = string.Empty;
    public FilterOperator Operator { get; set; }
    public object? Value { get; set; }
    public LogicalOperator? LogicalOperator { get; set; }
}

