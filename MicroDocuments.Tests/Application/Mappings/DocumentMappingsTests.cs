using FluentAssertions;
using MicroDocuments.Application.DTOs;
using MicroDocuments.Application.Mappings;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Enums;
using MicroDocuments.Tests.TestHelpers;
using Xunit;

namespace MicroDocuments.Tests.Application.Mappings;

public class DocumentMappingsTests
{
    [Fact]
    public void ToUploadResponseDto_Should_MapCorrectly()
    {
        // Arrange
        var document = new DocumentBuilder()
            .WithId(Guid.NewGuid())
            .Build();

        // Act
        var result = document.ToUploadResponseDto();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(document.Id.ToString());
    }

    [Fact]
    public void ToAssetDto_Should_MapAllProperties()
    {
        // Arrange
        var document = new DocumentBuilder()
            .WithId(Guid.NewGuid())
            .WithFilename("test.pdf")
            .WithContentType("application/pdf")
            .WithDocumentType(DocumentType.CONTRACT)
            .WithChannel(Channel.DIGITAL)
            .WithStatus(DocumentStatus.SENT)
            .WithCustomerId("customer-123")
            .WithCorrelationId("correlation-123")
            .WithUrl("https://example.com/document")
            .WithSize(1024)
            .WithUploadDate(DateTime.UtcNow)
            .Build();

        // Act
        var result = document.ToAssetDto();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(document.Id.ToString());
        result.Filename.Should().Be(document.Filename);
        result.ContentType.Should().Be(document.ContentType);
        result.DocumentType.Should().Be(document.DocumentType);
        result.Channel.Should().Be(document.Channel);
        result.Status.Should().Be(document.Status);
        result.CustomerId.Should().Be(document.CustomerId);
        result.CorrelationId.Should().Be(document.CorrelationId);
        result.Url.Should().Be(document.Url);
        result.Size.Should().Be(document.Size);
        result.UploadDate.Should().Be(document.UploadDate);
    }

    [Fact]
    public void ToAssetDtoQuery_Should_MapQueryable()
    {
        // Arrange
        var documents = new List<Document>
        {
            new DocumentBuilder().WithId(Guid.NewGuid()).Build(),
            new DocumentBuilder().WithId(Guid.NewGuid()).Build()
        };
        var queryable = documents.AsQueryable();

        // Act
        var result = queryable.ToAssetDtoQuery().ToList();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(documents[0].Id.ToString());
        result[1].Id.Should().Be(documents[1].Id.ToString());
    }

    [Fact]
    public void ToEntity_Should_MapFromDto_WithFileSize()
    {
        // Arrange
        var dto = MockDataFactory.CreateUploadRequestDto();
        var id = Guid.NewGuid();
        var uploadDate = DateTime.UtcNow;
        var fileSize = 2048L;

        // Act
        var result = dto.ToEntity(id, uploadDate, fileSize);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(id);
        result.Filename.Should().Be(dto.Filename);
        result.ContentType.Should().Be(dto.ContentType);
        result.DocumentType.Should().Be(dto.DocumentType);
        result.Channel.Should().Be(dto.Channel);
        result.CustomerId.Should().Be(dto.CustomerId);
        result.CorrelationId.Should().Be(dto.CorrelationId);
        result.Status.Should().Be(DocumentStatus.RECEIVED);
        result.Size.Should().Be(fileSize);
        result.UploadDate.Should().Be(uploadDate);
    }

    [Fact]
    public void ToEntity_Should_MapFromDto_WithoutFileSize()
    {
        // Arrange
        var dto = MockDataFactory.CreateUploadRequestDto();
        dto.FileSize = 1024L;
        var id = Guid.NewGuid();
        var uploadDate = DateTime.UtcNow;

        // Act
        var result = dto.ToEntity(id, uploadDate);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(id);
        result.Size.Should().Be(1024L); // Should use FileSize from DTO
    }

    [Fact]
    public void ToEntity_Should_UseFileSizeFromDto_When_Provided()
    {
        // Arrange
        var dto = MockDataFactory.CreateUploadRequestDto();
        dto.FileSize = 4096L;
        var id = Guid.NewGuid();
        var uploadDate = DateTime.UtcNow;
        var parameterFileSize = 2048L;

        // Act
        var result = dto.ToEntity(id, uploadDate, parameterFileSize);

        // Assert
        result.Should().NotBeNull();
        result.Size.Should().Be(4096L); // Should prefer FileSize from DTO
    }
}

