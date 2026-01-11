using FluentAssertions;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Enums;
using MicroDocuments.Tests.TestHelpers;

namespace MicroDocuments.Tests.Domain.Entities;

public class DocumentTests
{
    [Fact]
    public void Document_Should_Create_WithAllProperties()
    {
        // Arrange & Act
        var document = new DocumentBuilder()
            .WithFilename("test.pdf")
            .WithContentType("application/pdf")
            .WithDocumentType(DocumentType.CONTRACT)
            .WithChannel(Channel.DIGITAL)
            .WithStatus(DocumentStatus.RECEIVED)
            .WithSize(1024)
            .WithCustomerId("customer-123")
            .WithCorrelationId("correlation-123")
            .WithUrl("https://example.com/document")
            .Build();

        // Assert
        document.Filename.Should().Be("test.pdf");
        document.ContentType.Should().Be("application/pdf");
        document.DocumentType.Should().Be(DocumentType.CONTRACT);
        document.Channel.Should().Be(Channel.DIGITAL);
        document.Status.Should().Be(DocumentStatus.RECEIVED);
        document.Size.Should().Be(1024);
        document.CustomerId.Should().Be("customer-123");
        document.CorrelationId.Should().Be("correlation-123");
        document.Url.Should().Be("https://example.com/document");
        document.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Document_Should_AllowNull_OptionalProperties()
    {
        // Arrange & Act
        var document = new DocumentBuilder()
            .WithCustomerId(null)
            .WithCorrelationId(null)
            .WithUrl(null)
            .Build();

        // Assert
        document.CustomerId.Should().BeNull();
        document.CorrelationId.Should().BeNull();
        document.Url.Should().BeNull();
    }

    [Fact]
    public void Document_Should_HaveDefaultValues()
    {
        // Arrange & Act
        var document = new Document();

        // Assert
        document.Filename.Should().BeEmpty();
        document.ContentType.Should().BeEmpty();
        document.Id.Should().Be(Guid.Empty); // Guid por defecto es Empty
        document.Size.Should().Be(0);
    }
}

