using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MicroDocuments.Application.DTOs;
using MicroDocuments.Application.UseCases;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Ports;
using MicroDocuments.Infrastructure.Persistence;
using MicroDocuments.Tests.TestHelpers;
using Moq;
using Xunit;

namespace MicroDocuments.Tests.Application.UseCases;

public class SearchDocumentsUseCaseTests
{
    private readonly Mock<ILogger<SearchDocumentsUseCase>> _loggerMock;

    public SearchDocumentsUseCaseTests()
    {
        _loggerMock = new Mock<ILogger<SearchDocumentsUseCase>>();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnDocuments_When_NoFilters()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.AddRange(MockDataFactory.CreateDocuments(3));
        });

        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var apiKeySettings = InMemoryDbContextFactory.CreateApiKeySettings();
        var repository = new DocumentRepository(context);
        var useCase = new SearchDocumentsUseCase(repository, _loggerMock.Object);
        var searchDto = MockDataFactory.CreateSearchDocumentsDto();

        // Act
        var result = await useCase.ExecuteAsync(searchDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ApplyFilters_When_FilterStringProvided()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.Add(new DocumentBuilder().WithFilename("test1.pdf").Build());
            db.Documents.Add(new DocumentBuilder().WithFilename("test2.pdf").Build());
            db.Documents.Add(new DocumentBuilder().WithFilename("other.doc").Build());
        });

        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var apiKeySettings = InMemoryDbContextFactory.CreateApiKeySettings();
        var repository = new DocumentRepository(context);
        var useCase = new SearchDocumentsUseCase(repository, _loggerMock.Object);
        var searchDto = MockDataFactory.CreateSearchDocumentsDto();
        searchDto.Filename = "test";

        // Act
        var result = await useCase.ExecuteAsync(searchDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(d => d.Filename.Contains("test")).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ApplySorting_When_SortByProvided()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.Add(new DocumentBuilder().WithFilename("z.pdf").WithUploadDate(DateTime.UtcNow.AddDays(1)).Build());
            db.Documents.Add(new DocumentBuilder().WithFilename("a.pdf").WithUploadDate(DateTime.UtcNow).Build());
            db.Documents.Add(new DocumentBuilder().WithFilename("m.pdf").WithUploadDate(DateTime.UtcNow.AddDays(2)).Build());
        });

        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var apiKeySettings = InMemoryDbContextFactory.CreateApiKeySettings();
        var repository = new DocumentRepository(context);
        var useCase = new SearchDocumentsUseCase(repository, _loggerMock.Object);
        var searchDto = MockDataFactory.CreateSearchDocumentsDto(sortBy: "Filename", sortDirection: "ASC");

        // Act
        var result = await useCase.ExecuteAsync(searchDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnEmptyList_When_NoDocumentsMatch()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.Create();
        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var apiKeySettings = InMemoryDbContextFactory.CreateApiKeySettings();
        var repository = new DocumentRepository(context);
        var useCase = new SearchDocumentsUseCase(repository, _loggerMock.Object);
        var searchDto = MockDataFactory.CreateSearchDocumentsDto();
        searchDto.Filename = "nonexistent";

        // Act
        var result = await useCase.ExecuteAsync(searchDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_HandleDateRangeFilters()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-5);
        var endDate = DateTime.UtcNow;
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.Add(new DocumentBuilder().WithUploadDate(DateTime.UtcNow.AddDays(-3)).Build());
            db.Documents.Add(new DocumentBuilder().WithUploadDate(DateTime.UtcNow.AddDays(-7)).Build());
            db.Documents.Add(new DocumentBuilder().WithUploadDate(DateTime.UtcNow.AddDays(1)).Build());
        });

        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var apiKeySettings = InMemoryDbContextFactory.CreateApiKeySettings();
        var repository = new DocumentRepository(context);
        var useCase = new SearchDocumentsUseCase(repository, _loggerMock.Object);
        var searchDto = MockDataFactory.CreateSearchDocumentsDto(
            uploadDateStart: startDate,
            uploadDateEnd: endDate);

        // Act
        var result = await useCase.ExecuteAsync(searchDto);

        // Assert
        result.Should().NotBeNull();
    }
}

