using System.ComponentModel.DataAnnotations;

namespace MicroDocuments.Api.DTOs;

public class CreateApiKeyRequestDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }

    [Range(1, int.MaxValue)]
    public int RateLimitPerMinute { get; set; } = 100;
}

