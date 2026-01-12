using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MicroDocuments.Api.DTOs;
using MicroDocuments.Application.DTOs;
using MicroDocuments.Application.Pagination;
using MicroDocuments.Application.UseCases;
using MicroDocuments.Domain.Enums;
using MicroDocuments.Infrastructure.Configuration;

namespace MicroDocuments.Api.Controllers;

[ApiController]
[Route("api/bhd/mgmt/1/documents")]
public class DocumentsController : ControllerBase
{
    private readonly UploadDocumentUseCase _uploadUseCase;
    private readonly SearchDocumentsUseCase _searchUseCase;
    private readonly SearchDocumentsPagedUseCase _searchPagedUseCase;
    private readonly ILogger<DocumentsController> _logger;
    private readonly FileUploadSettings _fileUploadSettings;
    private readonly IWebHostEnvironment _environment;

    public DocumentsController(
        UploadDocumentUseCase uploadUseCase,
        SearchDocumentsUseCase searchUseCase,
        SearchDocumentsPagedUseCase searchPagedUseCase,
        ILogger<DocumentsController> logger,
        IOptions<FileUploadSettings> fileUploadSettings,
        IWebHostEnvironment environment)
    {
        _uploadUseCase = uploadUseCase;
        _searchUseCase = searchUseCase;
        _searchPagedUseCase = searchPagedUseCase;
        _logger = logger;
        _fileUploadSettings = fileUploadSettings.Value;
        _environment = environment;
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

        if (formDto.File.Length > _fileUploadSettings.MaxFileSizeBytes)
        {
            return BadRequest($"File size exceeds maximum allowed size of {_fileUploadSettings.MaxFileSizeMB} MB");
        }

        var filename = string.IsNullOrWhiteSpace(formDto.Filename)
            ? formDto.File.FileName
            : formDto.Filename;

        var contentType = string.IsNullOrWhiteSpace(formDto.ContentType)
            ? formDto.File.ContentType
            : formDto.ContentType;

        try
        {
            await using var fileStream = formDto.File.OpenReadStream();
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
            var errorMessage = _environment.IsDevelopment() 
                ? ex.Message 
                : "An unexpected error occurred. Please try again later.";
            return StatusCode(500, errorMessage);
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
            var errorMessage = _environment.IsDevelopment() 
                ? ex.Message 
                : "An unexpected error occurred. Please try again later.";
            return StatusCode(500, errorMessage);
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
            paginationRequest.SortBy = "Created";
        }
        
        if (string.IsNullOrWhiteSpace(paginationRequest.SortDirection))
        {
            paginationRequest.SortDirection = "DESC";
        }

        try
        {
            var result = await _searchPagedUseCase.ExecuteAsync(paginationRequest, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DocumentsController.SearchDocumentsPaged - Error searching documents");
            var errorMessage = _environment.IsDevelopment() 
                ? ex.Message 
                : "An unexpected error occurred. Please try again later.";
            return StatusCode(500, errorMessage);
        }
    }
}

