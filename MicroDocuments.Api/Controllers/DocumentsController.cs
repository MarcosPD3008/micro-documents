using Microsoft.AspNetCore.Mvc;
using MicroDocuments.Api.DTOs;
using MicroDocuments.Application.DTOs;
using MicroDocuments.Application.Pagination;
using MicroDocuments.Application.UseCases;
using MicroDocuments.Domain.Enums;

namespace MicroDocuments.Api.Controllers;

[ApiController]
[Route("api/bhd/mgmt/1/documents")]
public class DocumentsController : ControllerBase
{
    private readonly UploadDocumentUseCase _uploadUseCase;
    private readonly SearchDocumentsUseCase _searchUseCase;
    private readonly SearchDocumentsPagedUseCase _searchPagedUseCase;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        UploadDocumentUseCase uploadUseCase,
        SearchDocumentsUseCase searchUseCase,
        SearchDocumentsPagedUseCase searchPagedUseCase,
        ILogger<DocumentsController> logger)
    {
        _uploadUseCase = uploadUseCase;
        _searchUseCase = searchUseCase;
        _searchPagedUseCase = searchPagedUseCase;
        _logger = logger;
    }

    [HttpPost("actions/upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentUploadResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadDocument(
        [FromForm] DocumentUploadFormDto formDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (formDto.File == null || formDto.File.Length == 0)
        {
            return BadRequest("File is required");
        }

        var filename = string.IsNullOrWhiteSpace(formDto.Filename)
            ? formDto.File.FileName
            : formDto.Filename;

        var contentType = string.IsNullOrWhiteSpace(formDto.ContentType)
            ? formDto.File.ContentType
            : formDto.ContentType;

        try
        {
            // Use streaming directly from IFormFile
            var fileStream = formDto.File.OpenReadStream();
            var fileSize = formDto.File.Length;

            var request = new DocumentUploadRequestDto
            {
                Filename = filename,
                FileStream = fileStream,
                FileSize = fileSize,
                ContentType = contentType,
                DocumentType = formDto.DocumentType,
                Channel = formDto.Channel,
                CustomerId = formDto.CustomerId,
                CorrelationId = formDto.CorrelationId
            };

            var response = await _uploadUseCase.ExecuteStreamAsync(request, cancellationToken);
            return Accepted(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DocumentsController.UploadDocument - Error uploading document");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<DocumentAssetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchDocuments(
        [FromQuery] SearchDocumentsDto searchDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var results = await _searchUseCase.ExecuteAsync(searchDto, cancellationToken);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DocumentsController.SearchDocuments - Error searching documents");
            return StatusCode(500, "An unexpected error occurred");
        }
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(PaginationResponse<DocumentAssetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchDocumentsPaged(
        [FromQuery] PaginationRequest paginationRequest,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paginationRequest.SortBy))
        {
            return BadRequest("SortBy is required");
        }

        try
        {
            var result = await _searchPagedUseCase.ExecuteAsync(paginationRequest, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DocumentsController.SearchDocumentsPaged - Error searching documents");
            return StatusCode(500, "An unexpected error occurred");
        }
    }
}

