using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MicroDocuments.Api.Controllers;
using MicroDocuments.Api.DTOs;
using MicroDocuments.Application.DTOs;
using MicroDocuments.Application.Pagination;
using MicroDocuments.Application.UseCases;
using MicroDocuments.Domain.Enums;
using MicroDocuments.Domain.Ports;
using MicroDocuments.Tests.TestHelpers;
using Moq;
using System.Text;
using Xunit;

namespace MicroDocuments.Tests.Api.Controllers;

public class DocumentsControllerTests
{
    private readonly Mock<UploadDocumentUseCase> _uploadUseCaseMock;
    private readonly Mock<SearchDocumentsUseCase> _searchUseCaseMock;
    private readonly Mock<SearchDocumentsPagedUseCase> _searchPagedUseCaseMock;
    private readonly Mock<ILogger<DocumentsController>> _loggerMock;
    private readonly DocumentsController _controller;

    public DocumentsControllerTests()
    {
        _uploadUseCaseMock = new Mock<UploadDocumentUseCase>(
            Mock.Of<IDocumentRepository>(),
            Mock.Of<IFileStorage>(),
            Mock.Of<ILogger<UploadDocumentUseCase>>());
        _searchUseCaseMock = new Mock<SearchDocumentsUseCase>(
            Mock.Of<IDocumentRepository>(),
            Mock.Of<ILogger<SearchDocumentsUseCase>>());
        _searchPagedUseCaseMock = new Mock<SearchDocumentsPagedUseCase>(
            Mock.Of<IDocumentRepository>(),
            Mock.Of<ILogger<SearchDocumentsPagedUseCase>>());
        _loggerMock = new Mock<ILogger<DocumentsController>>();
        _controller = new DocumentsController(
            _uploadUseCaseMock.Object,
            _searchUseCaseMock.Object,
            _searchPagedUseCaseMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task UploadDocument_Should_ReturnAccepted_When_ValidRequest()
    {
        // Arrange
        var formDto = new DocumentUploadFormDto
        {
            File = CreateMockFormFile("test.pdf", "application/pdf", new byte[] { 1, 2, 3, 4 }),
            DocumentType = DocumentType.CONTRACT,
            Channel = Channel.DIGITAL
        };

        var responseDto = new DocumentUploadResponseDto { Id = Guid.NewGuid().ToString() };
        _uploadUseCaseMock
            .Setup(x => x.ExecuteStreamAsync(It.IsAny<DocumentUploadRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.UploadDocument(formDto);

        // Assert
        result.Should().NotBeNull();
        var acceptedResult = result.Should().BeOfType<AcceptedResult>().Subject;
        _uploadUseCaseMock.Verify(x => x.ExecuteStreamAsync(It.IsAny<DocumentUploadRequestDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocument_Should_ReturnBadRequest_When_FileIsNull()
    {
        // Arrange
        var formDto = new DocumentUploadFormDto
        {
            File = null!,
            DocumentType = DocumentType.CONTRACT,
            Channel = Channel.DIGITAL
        };

        // Act
        var result = await _controller.UploadDocument(formDto);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("File is required");
    }

    [Fact]
    public async Task UploadDocument_Should_ReturnBadRequest_When_ModelStateInvalid()
    {
        // Arrange
        var formDto = new DocumentUploadFormDto();
        _controller.ModelState.AddModelError("DocumentType", "DocumentType is required");

        // Act
        var result = await _controller.UploadDocument(formDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UploadDocument_Should_UseFilenameFromFile_When_FilenameNotProvided()
    {
        // Arrange
        var formDto = new DocumentUploadFormDto
        {
            File = CreateMockFormFile("original.pdf", "application/pdf", new byte[] { 1, 2, 3, 4 }),
            DocumentType = DocumentType.CONTRACT,
            Channel = Channel.DIGITAL
        };

        var responseDto = new DocumentUploadResponseDto { Id = Guid.NewGuid().ToString() };
        _uploadUseCaseMock
            .Setup(x => x.ExecuteStreamAsync(It.Is<DocumentUploadRequestDto>(d => d.Filename == "original.pdf"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        await _controller.UploadDocument(formDto);

        // Assert
        _uploadUseCaseMock.Verify(x => x.ExecuteStreamAsync(
            It.Is<DocumentUploadRequestDto>(d => d.Filename == "original.pdf"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocument_Should_UseContentTypeFromFile_When_ContentTypeNotProvided()
    {
        // Arrange
        var formDto = new DocumentUploadFormDto
        {
            File = CreateMockFormFile("test.pdf", "application/pdf", new byte[] { 1, 2, 3, 4 }),
            DocumentType = DocumentType.CONTRACT,
            Channel = Channel.DIGITAL
        };

        var responseDto = new DocumentUploadResponseDto { Id = Guid.NewGuid().ToString() };
        _uploadUseCaseMock
            .Setup(x => x.ExecuteStreamAsync(It.IsAny<DocumentUploadRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        await _controller.UploadDocument(formDto);

        // Assert
        _uploadUseCaseMock.Verify(x => x.ExecuteStreamAsync(
            It.Is<DocumentUploadRequestDto>(d => d.ContentType == "application/pdf"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocument_Should_Return500_When_UseCaseThrows()
    {
        // Arrange
        var formDto = new DocumentUploadFormDto
        {
            File = CreateMockFormFile("test.pdf", "application/pdf", new byte[] { 1, 2, 3, 4 }),
            DocumentType = DocumentType.CONTRACT,
            Channel = Channel.DIGITAL
        };

        _uploadUseCaseMock
            .Setup(x => x.ExecuteStreamAsync(It.IsAny<DocumentUploadRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _controller.UploadDocument(formDto);

        // Assert
        result.Should().NotBeNull();
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task SearchDocuments_Should_ReturnOk_When_ValidRequest()
    {
        // Arrange
        var searchDto = MockDataFactory.CreateSearchDocumentsDto();
        var documents = new List<DocumentAssetDto>
        {
            new DocumentAssetDto { Id = Guid.NewGuid().ToString(), Filename = "test1.pdf" },
            new DocumentAssetDto { Id = Guid.NewGuid().ToString(), Filename = "test2.pdf" }
        };

        _searchUseCaseMock
            .Setup(x => x.ExecuteAsync(It.IsAny<SearchDocumentsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _controller.SearchDocuments(searchDto);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(documents);
    }

    [Fact]
    public async Task SearchDocuments_Should_ReturnBadRequest_When_ModelStateInvalid()
    {
        // Arrange
        var searchDto = MockDataFactory.CreateSearchDocumentsDto();
        _controller.ModelState.AddModelError("SortBy", "SortBy is invalid");

        // Act
        var result = await _controller.SearchDocuments(searchDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SearchDocuments_Should_Return500_When_UseCaseThrows()
    {
        // Arrange
        var searchDto = MockDataFactory.CreateSearchDocumentsDto();
        _searchUseCaseMock
            .Setup(x => x.ExecuteAsync(It.IsAny<SearchDocumentsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _controller.SearchDocuments(searchDto);

        // Assert
        result.Should().NotBeNull();
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task SearchDocumentsPaged_Should_ReturnOk_When_ValidRequest()
    {
        // Arrange
        var paginationRequest = MockDataFactory.CreatePaginationRequest();
        var paginationResponse = new PaginationResponse<DocumentAssetDto>
        {
            Content = new List<DocumentAssetDto>
            {
                new DocumentAssetDto { Id = Guid.NewGuid().ToString(), Filename = "test1.pdf" }
            },
            Total = 1,
            NextPage = false
        };

        _searchPagedUseCaseMock
            .Setup(x => x.ExecuteAsync(It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        var result = await _controller.SearchDocumentsPaged(paginationRequest);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(paginationResponse);
    }

    [Fact]
    public async Task SearchDocumentsPaged_Should_ReturnBadRequest_When_SortByIsMissing()
    {
        // Arrange
        var paginationRequest = MockDataFactory.CreatePaginationRequest();
        paginationRequest.SortBy = null!;

        // Act
        var result = await _controller.SearchDocumentsPaged(paginationRequest);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("SortBy is required");
    }

    [Fact]
    public async Task SearchDocumentsPaged_Should_Return500_When_UseCaseThrows()
    {
        // Arrange
        var paginationRequest = MockDataFactory.CreatePaginationRequest();
        _searchPagedUseCaseMock
            .Setup(x => x.ExecuteAsync(It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _controller.SearchDocumentsPaged(paginationRequest);

        // Assert
        result.Should().NotBeNull();
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    private static IFormFile CreateMockFormFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}

