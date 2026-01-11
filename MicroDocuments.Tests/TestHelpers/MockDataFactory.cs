using MicroDocuments.Application.DTOs;
using MicroDocuments.Application.Pagination;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Enums;
using MicroDocuments.Tests.TestHelpers;

namespace MicroDocuments.Tests.TestHelpers;

public static class MockDataFactory
{
    public static Document CreateDocument(
        Guid? id = null,
        DocumentStatus status = DocumentStatus.RECEIVED,
        string? customerId = null,
        string? correlationId = null)
    {
        return new DocumentBuilder()
            .WithId(id ?? Guid.NewGuid())
            .WithStatus(status)
            .WithCustomerId(customerId)
            .WithCorrelationId(correlationId)
            .Build();
    }

    public static List<Document> CreateDocuments(int count)
    {
        var documents = new List<Document>();
        for (int i = 0; i < count; i++)
        {
            documents.Add(CreateDocument());
        }
        return documents;
    }

    public static DocumentUploadRequestDto CreateUploadRequestDto(
        string? filename = null,
        string? encodedFile = null,
        Stream? fileStream = null,
        long? fileSize = null)
    {
        return new DocumentUploadRequestDto
        {
            Filename = filename ?? "test-document.pdf",
            EncodedFile = encodedFile ?? Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
            FileStream = fileStream,
            FileSize = fileSize,
            ContentType = "application/pdf",
            DocumentType = DocumentType.CONTRACT,
            Channel = Channel.DIGITAL,
            CustomerId = "customer-123",
            CorrelationId = "correlation-123"
        };
    }

    public static SearchDocumentsDto CreateSearchDocumentsDto(
        string? sortBy = null,
        string? sortDirection = null,
        DateTime? uploadDateStart = null,
        DateTime? uploadDateEnd = null)
    {
        return new SearchDocumentsDto
        {
            SortBy = sortBy ?? string.Empty,
            SortDirection = sortDirection ?? "ASC",
            UploadDateStart = uploadDateStart,
            UploadDateEnd = uploadDateEnd
        };
    }

    public static PaginationRequest CreatePaginationRequest(
        int page = 1,
        int pageSize = 10,
        string? sortBy = null,
        string? sortDirection = null,
        string? filter = null)
    {
        return new PaginationRequest
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy ?? "UploadDate",
            SortDirection = sortDirection ?? "ASC",
            Filter = filter
        };
    }

    public static byte[] CreateTestFileContent(int sizeInBytes = 1024)
    {
        var content = new byte[sizeInBytes];
        Random.Shared.NextBytes(content);
        return content;
    }

    public static Stream CreateTestStream(int sizeInBytes = 1024)
    {
        var content = CreateTestFileContent(sizeInBytes);
        return new MemoryStream(content);
    }
}

