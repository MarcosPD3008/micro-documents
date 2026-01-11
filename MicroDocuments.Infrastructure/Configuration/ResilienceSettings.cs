namespace MicroDocuments.Infrastructure.Configuration;

public class ResilienceSettings
{
    public const string SectionName = "Resilience";

    public RateLimiterSettings RateLimiter { get; set; } = new();
    public RetryPolicySettings RetryPolicy { get; set; } = new();
}

public class RateLimiterSettings
{
    public bool Enabled { get; set; } = true;
}

public class RetryPolicySettings
{
    public int MaxRetryAttempts { get; set; } = 3;
    public bool Enabled { get; set; } = true;
}

