using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Infrastructure.Configuration;
using MicroDocuments.Infrastructure.ExternalServices;
using MicroDocuments.Tests.TestHelpers;
using Moq;
using Xunit;

namespace MicroDocuments.Tests.Infrastructure.ExternalServices;

public class DocumentPublisherMockTests
{
    private readonly Mock<ILogger<DocumentPublisherMock>> _loggerMock;
    private readonly DocumentPublisherSettings _settings;
    private readonly DocumentPublisherMock _publisher;

    public DocumentPublisherMockTests()
    {
        _loggerMock = new Mock<ILogger<DocumentPublisherMock>>();
        _settings = new DocumentPublisherSettings
        {
            Url = "https://example.com/api/documents"
        };
        _publisher = new DocumentPublisherMock(
            Options.Create(_settings),
            _loggerMock.Object);
    }

    [Fact]
    public async Task PublishAsync_Should_ReturnMockUrl()
    {
        // Arrange
        var document = new DocumentBuilder()
            .WithId(Guid.NewGuid())
            .WithFilename("test.pdf")
            .Build();
        var content = new byte[] { 1, 2, 3, 4 };

        // Act
        var result = await _publisher.PublishAsync(document, content);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be($"{_settings.Url}/documents/{document.Id}");
    }

    [Fact]
    public async Task PublishStreamAsync_Should_ReturnMockUrl()
    {
        // Arrange
        var document = new DocumentBuilder()
            .WithId(Guid.NewGuid())
            .WithFilename("test.pdf")
            .Build();
        var stream = MockDataFactory.CreateTestStream(1024);

        // Act
        var result = await _publisher.PublishStreamAsync(document, stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be($"{_settings.Url}/documents/{document.Id}");
    }
}




