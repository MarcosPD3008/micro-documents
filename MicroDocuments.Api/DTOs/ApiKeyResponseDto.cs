namespace MicroDocuments.Api.DTOs;

public class ApiKeyResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int RateLimitPerMinute { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
}

