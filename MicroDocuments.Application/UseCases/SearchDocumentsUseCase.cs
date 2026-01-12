using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MicroDocuments.Application.DTOs;
using MicroDocuments.Application.Extensions;
using MicroDocuments.Application.Mappings;
using MicroDocuments.Application.Pagination;
using MicroDocuments.Application.Sorting;
using MicroDocuments.Domain.Ports;

namespace MicroDocuments.Application.UseCases;

public class SearchDocumentsUseCase
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<SearchDocumentsUseCase> _logger;

    public SearchDocumentsUseCase(
        IDocumentRepository repository,
        ILogger<SearchDocumentsUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public virtual async Task<List<DocumentAssetDto>> ExecuteAsync(
        SearchDocumentsDto searchDto,
        CancellationToken cancellationToken = default)
    {
        var query = _repository.GetAll();

        var filterString = BuildFilterString(searchDto);
        if (!string.IsNullOrWhiteSpace(filterString))
        {
            query = query.ApplyFilters(filterString);
        }

        var sortRequest = new SortRequest
        {
            SortBy = string.IsNullOrWhiteSpace(searchDto.SortBy) ? "Created" : searchDto.SortBy,
            SortDirection = string.IsNullOrWhiteSpace(searchDto.SortDirection) ? "DESC" : searchDto.SortDirection
        };
        query = query.ApplySorting(sortRequest);

        var documents = await query.ToListAsync(cancellationToken);

        _logger.LogInformation(
            "SearchDocumentsUseCase.ExecuteAsync - Documents found: {Count}",
            documents.Count);

        return documents.Select(d => d.ToAssetDto()).ToList();
    }

    private static string? BuildFilterString(SearchDocumentsDto searchDto)
    {
        var filters = new List<string>();

        if (searchDto.UploadDateStart.HasValue)
        {
            filters.Add($"uploadDate ge '{searchDto.UploadDateStart.Value:yyyy-MM-ddTHH:mm:ss}'");
        }

        if (searchDto.UploadDateEnd.HasValue)
        {
            filters.Add($"uploadDate le '{searchDto.UploadDateEnd.Value:yyyy-MM-ddTHH:mm:ss}'");
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Filename))
        {
            filters.Add($"filename contains '{searchDto.Filename}'");
        }

        if (!string.IsNullOrWhiteSpace(searchDto.ContentType))
        {
            filters.Add($"contentType eq '{searchDto.ContentType}'");
        }

        if (searchDto.DocumentType.HasValue)
        {
            filters.Add($"documentType eq '{searchDto.DocumentType.Value}'");
        }

        if (searchDto.Status.HasValue)
        {
            filters.Add($"status eq '{searchDto.Status.Value}'");
        }

        if (!string.IsNullOrWhiteSpace(searchDto.CustomerId))
        {
            filters.Add($"customerId eq '{searchDto.CustomerId}'");
        }

        if (searchDto.Channel.HasValue)
        {
            filters.Add($"channel eq '{searchDto.Channel.Value}'");
        }

        return filters.Count > 0 ? string.Join(" and ", filters) : null;
    }
}

