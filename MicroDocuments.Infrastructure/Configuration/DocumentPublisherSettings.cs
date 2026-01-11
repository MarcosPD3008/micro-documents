namespace MicroDocuments.Infrastructure.Configuration;

public class DocumentPublisherSettings
{
    public const string SectionName = "DocumentPublisher";
    
    public string Url { get; set; } = string.Empty;
    public bool UseMock { get; set; } = true;
}

