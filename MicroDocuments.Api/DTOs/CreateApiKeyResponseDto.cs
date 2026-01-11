namespace MicroDocuments.Api.DTOs;

public class CreateApiKeyResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty; // Only returned once when created
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int RateLimitPerMinute { get; set; }
    public DateTime Created { get; set; }
}

