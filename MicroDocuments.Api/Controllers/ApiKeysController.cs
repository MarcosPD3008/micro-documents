using Microsoft.AspNetCore.Mvc;
using MicroDocuments.Api.DTOs;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Ports;
using MicroDocuments.Infrastructure.Services;

namespace MicroDocuments.Api.Controllers;

[ApiController]
[Route("api/bhd/mgmt/1/apikeys")]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IApiKeyCacheService _cacheService;
    private readonly ApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(
        IApiKeyRepository apiKeyRepository,
        IApiKeyCacheService cacheService,
        ApiKeyService apiKeyService,
        ILogger<ApiKeysController> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _cacheService = cacheService;
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateApiKeyResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateApiKey(
        [FromBody] CreateApiKeyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Generate a new API key
            var apiKeyValue = _apiKeyService.GenerateApiKey();
            var keyHash = _apiKeyService.HashApiKey(apiKeyValue);

            // Check if hash already exists (very unlikely but possible)
            var existing = await _apiKeyRepository.GetByKeyHashAsync(keyHash, cancellationToken);
            if (existing != null)
            {
                _logger.LogWarning("ApiKeysController.CreateApiKey - Generated API key hash collision, retrying");
                // Retry once
                apiKeyValue = _apiKeyService.GenerateApiKey();
                keyHash = _apiKeyService.HashApiKey(apiKeyValue);
            }

            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                KeyHash = keyHash,
                IsActive = true,
                ExpiresAt = request.ExpiresAt,
                RateLimitPerMinute = request.RateLimitPerMinute,
                Created = DateTime.UtcNow
            };

            var saved = await _apiKeyRepository.CreateAsync(apiKey, cancellationToken);

            // Update cache
            _cacheService.AddOrUpdate(saved);

            var response = new CreateApiKeyResponseDto
            {
                Id = saved.Id,
                Name = saved.Name,
                ApiKey = apiKeyValue, // Return the plain API key only once
                IsActive = saved.IsActive,
                ExpiresAt = saved.ExpiresAt,
                RateLimitPerMinute = saved.RateLimitPerMinute,
                Created = saved.Created
            };

            _logger.LogInformation("ApiKeysController.CreateApiKey - Created API key {Id} with name {Name}", saved.Id, saved.Name);

            return CreatedAtAction(
                nameof(GetApiKey),
                new { id = saved.Id },
                response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApiKeysController.CreateApiKey - Error creating API key");
            return StatusCode(500, "An unexpected error occurred while creating the API key");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ApiKeyResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListApiKeys(CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKeys = await _apiKeyRepository.GetAllActiveAsync(cancellationToken);
            
            var response = apiKeys.Select(k => new ApiKeyResponseDto
            {
                Id = k.Id,
                Name = k.Name,
                IsActive = k.IsActive,
                ExpiresAt = k.ExpiresAt,
                LastUsedAt = k.LastUsedAt,
                RateLimitPerMinute = k.RateLimitPerMinute,
                Created = k.Created,
                Updated = k.Updated
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApiKeysController.ListApiKeys - Error listing API keys");
            return StatusCode(500, "An unexpected error occurred while listing API keys");
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiKeyResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetApiKey(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await _apiKeyRepository.GetByIdAsync(id, cancellationToken);
            
            if (apiKey == null)
            {
                return NotFound();
            }

            var response = new ApiKeyResponseDto
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                IsActive = apiKey.IsActive,
                ExpiresAt = apiKey.ExpiresAt,
                LastUsedAt = apiKey.LastUsedAt,
                RateLimitPerMinute = apiKey.RateLimitPerMinute,
                Created = apiKey.Created,
                Updated = apiKey.Updated
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApiKeysController.GetApiKey - Error getting API key {Id}", id);
            return StatusCode(500, "An unexpected error occurred while getting the API key");
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteApiKey(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await _apiKeyRepository.GetByIdAsync(id, cancellationToken);
            
            if (apiKey == null)
            {
                return NotFound();
            }

            apiKey.Deleted = DateTime.UtcNow;
            apiKey.IsActive = false;
            await _apiKeyRepository.UpdateAsync(apiKey, cancellationToken);

            // Remove from cache
            _cacheService.Remove(apiKey.KeyHash);

            _logger.LogInformation("ApiKeysController.DeleteApiKey - Deleted API key {Id}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApiKeysController.DeleteApiKey - Error deleting API key {Id}", id);
            return StatusCode(500, "An unexpected error occurred while deleting the API key");
        }
    }
}

