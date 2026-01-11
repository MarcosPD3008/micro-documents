using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Enums;

namespace MicroDocuments.Tests.TestHelpers;

public class DocumentBuilder
{
    private Document _document;

    public DocumentBuilder()
    {
        _document = new Document
        {
            Id = Guid.NewGuid(),
            Filename = "test-document.pdf",
            ContentType = "application/pdf",
            DocumentType = DocumentType.CONTRACT,
            Channel = Channel.DIGITAL,
            Status = DocumentStatus.RECEIVED,
            Size = 1024,
            UploadDate = DateTime.UtcNow,
            Created = DateTime.UtcNow
        };
    }

    public DocumentBuilder WithId(Guid id)
    {
        _document.Id = id;
        return this;
    }

    public DocumentBuilder WithFilename(string filename)
    {
        _document.Filename = filename;
        return this;
    }

    public DocumentBuilder WithContentType(string contentType)
    {
        _document.ContentType = contentType;
        return this;
    }

    public DocumentBuilder WithDocumentType(DocumentType documentType)
    {
        _document.DocumentType = documentType;
        return this;
    }

    public DocumentBuilder WithChannel(Channel channel)
    {
        _document.Channel = channel;
        return this;
    }

    public DocumentBuilder WithStatus(DocumentStatus status)
    {
        _document.Status = status;
        return this;
    }

    public DocumentBuilder WithCustomerId(string? customerId)
    {
        _document.CustomerId = customerId;
        return this;
    }

    public DocumentBuilder WithCorrelationId(string? correlationId)
    {
        _document.CorrelationId = correlationId;
        return this;
    }

    public DocumentBuilder WithUrl(string? url)
    {
        _document.Url = url;
        return this;
    }

    public DocumentBuilder WithSize(long size)
    {
        _document.Size = size;
        return this;
    }

    public DocumentBuilder WithUploadDate(DateTime uploadDate)
    {
        _document.UploadDate = uploadDate;
        return this;
    }

    public Document Build()
    {
        return _document;
    }
}

