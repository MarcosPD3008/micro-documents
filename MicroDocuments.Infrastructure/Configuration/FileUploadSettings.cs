namespace MicroDocuments.Infrastructure.Configuration;

public class FileUploadSettings
{
    public const string SectionName = "FileUpload";
    
    public int MaxFileSizeMB { get; set; } = 100;
    
    public long MaxFileSizeBytes => MaxFileSizeMB * 1024L * 1024L;
}

