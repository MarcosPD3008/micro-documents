using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace MicroDocuments.Infrastructure.Services;

public class ApiKeyService
{
    private readonly string _secretKey;

    public ApiKeyService(IConfiguration configuration)
    {
        _secretKey = configuration["ApiKey:SecretKey"] 
            ?? throw new InvalidOperationException("ApiKey:SecretKey configuration is required");
    }

    public string HashApiKey(string apiKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hashBytes);
    }

    public bool ValidateApiKey(string apiKey, string storedHash)
    {
        var computedHash = HashApiKey(apiKey);
        return computedHash == storedHash;
    }

    public string GenerateApiKey()
    {
        // Generate a secure random API key
        // Format: prefix-randomguid-base64random
        const string prefix = "bhd-";
        var guid = Guid.NewGuid().ToString("N");
        var randomBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        var randomPart = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "")
            .Substring(0, 22); // Take first 22 chars for consistent length
        
        return $"{prefix}{guid}-{randomPart}";
    }
}

