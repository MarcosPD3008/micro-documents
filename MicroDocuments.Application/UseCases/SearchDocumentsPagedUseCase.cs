using Microsoft.Extensions.Logging;
using MicroDocuments.Application.DTOs;
using MicroDocuments.Application.Extensions;
using MicroDocuments.Application.Mappings;
using MicroDocuments.Application.Pagination;
using MicroDocuments.Application.Sorting;
using MicroDocuments.Domain.Ports;

namespace MicroDocuments.Application.UseCases;

public class SearchDocumentsPagedUseCase
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<SearchDocumentsPagedUseCase> _logger;

    public SearchDocumentsPagedUseCase(
        IDocumentRepository repository,
        ILogger<SearchDocumentsPagedUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public virtual async Task<PaginationResponse<DocumentAssetDto>> ExecuteAsync(
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken = default)
    {
        var query = _repository.GetAll();

        if (!string.IsNullOrWhiteSpace(paginationRequest.Filter))
        {
            query = query.ApplyFilters(paginationRequest.Filter);
        }

        var sortRequest = new SortRequest
        {
            SortBy = paginationRequest.SortBy,
            SortDirection = paginationRequest.SortDirection
        };
        query = query.ApplySorting(sortRequest);

        var queryDto = query.ToAssetDtoQuery();

        var pagination = new PaginationRequest
        {
            Page = paginationRequest.Page,
            PageSize = paginationRequest.PageSize
        };

        var result = await queryDto.ToPagedAsync(pagination, cancellationToken);

        _logger.LogInformation(
            "SearchDocumentsPagedUseCase.ExecuteAsync - Documents found: {Count}, Total: {Total}, Page: {Page}",
            result.Content.Count, result.Total, paginationRequest.Page);

        return result;
    }
}

