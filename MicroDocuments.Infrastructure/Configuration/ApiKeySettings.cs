namespace MicroDocuments.Infrastructure.Configuration;

public class ApiKeySettings
{
    public const string SectionName = "ApiKey";
    
    public string SecretKey { get; set; } = string.Empty;
    public string MasterKey { get; set; } = string.Empty;
    public bool GlobalFilter { get; set; } = false;
}


