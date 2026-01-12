using FluentAssertions;
using Microsoft.Extensions.Logging;
using MicroDocuments.Application.DTOs;
using MicroDocuments.Application.UseCases;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Ports;
using MicroDocuments.Tests.TestHelpers;
using Moq;
using Xunit;

namespace MicroDocuments.Tests.Application.UseCases;

public class UploadDocumentUseCaseTests
{
    private readonly Mock<IDocumentRepository> _repositoryMock;
    private readonly Mock<IFileStorage> _fileStorageMock;
    private readonly Mock<ILogger<UploadDocumentUseCase>> _loggerMock;
    private readonly UploadDocumentUseCase _useCase;

    public UploadDocumentUseCaseTests()
    {
        _repositoryMock = new Mock<IDocumentRepository>();
        _fileStorageMock = new Mock<IFileStorage>();
        _loggerMock = new Mock<ILogger<UploadDocumentUseCase>>();
        _useCase = new UploadDocumentUseCase(
            _repositoryMock.Object,
            _fileStorageMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_Should_SaveDocument_When_ValidRequest()
    {
        // Arrange
        var request = MockDataFactory.CreateUploadRequestDto();
        var savedDocument = new DocumentBuilder()
            .WithId(Guid.NewGuid())
            .WithFilename(request.Filename)
            .Build();

        _fileStorageMock
            .Setup(x => x.SaveAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedDocument);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(savedDocument.Id.ToString());
        _fileStorageMock.Verify(x => x.SaveAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.CreateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ThrowException_When_EncodedFileIsInvalid()
    {
        // Arrange
        var request = MockDataFactory.CreateUploadRequestDto();
        request.EncodedFile = "invalid-base64!";

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => _useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task ExecuteAsync_Should_CleanupFile_When_SaveFails()
    {
        // Arrange
        var request = MockDataFactory.CreateUploadRequestDto();
        var documentId = Guid.NewGuid();

        _fileStorageMock
            .Setup(x => x.SaveAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        _fileStorageMock
            .Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _useCase.ExecuteAsync(request));
        _fileStorageMock.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteStreamAsync_Should_SaveDocument_When_ValidStream()
    {
        // Arrange
        var stream = MockDataFactory.CreateTestStream();
        var request = MockDataFactory.CreateUploadRequestDto(fileStream: stream, fileSize: 1024);
        var savedDocument = new DocumentBuilder()
            .WithId(Guid.NewGuid())
            .WithFilename(request.Filename)
            .Build();

        _fileStorageMock
            .Setup(x => x.SaveFromStreamAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedDocument);

        // Act
        var result = await _useCase.ExecuteStreamAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(savedDocument.Id.ToString());
        _fileStorageMock.Verify(x => x.SaveFromStreamAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.CreateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteStreamAsync_Should_ThrowException_When_FileStreamIsNull()
    {
        // Arrange
        var request = MockDataFactory.CreateUploadRequestDto();
        request.FileStream = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteStreamAsync(request));
    }

    [Fact]
    public async Task ExecuteStreamAsync_Should_CleanupFile_When_SaveFails()
    {
        // Arrange
        var stream = MockDataFactory.CreateTestStream();
        var request = MockDataFactory.CreateUploadRequestDto(fileStream: stream, fileSize: 1024);

        _fileStorageMock
            .Setup(x => x.SaveFromStreamAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage error"));

        _fileStorageMock
            .Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _useCase.ExecuteStreamAsync(request));
        _fileStorageMock.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}






