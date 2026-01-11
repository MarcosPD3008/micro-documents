namespace MicroDocuments.Infrastructure.Middleware;

public class ErrorResponseDto
{
    public string Error { get; set; } = string.Empty;
    public string? Message { get; set; }
    public int StatusCode { get; set; }
}

