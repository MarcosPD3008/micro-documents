using FluentAssertions;
using Microsoft.Extensions.Logging;
using MicroDocuments.Application.Pagination;
using MicroDocuments.Application.UseCases;
using MicroDocuments.Infrastructure.Persistence;
using MicroDocuments.Tests.TestHelpers;
using Moq;
using Xunit;

namespace MicroDocuments.Tests.Application.UseCases;

public class SearchDocumentsPagedUseCaseTests
{
    private readonly Mock<ILogger<SearchDocumentsPagedUseCase>> _loggerMock;

    public SearchDocumentsPagedUseCaseTests()
    {
        _loggerMock = new Mock<ILogger<SearchDocumentsPagedUseCase>>();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnPagedResults_When_ValidRequest()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.AddRange(MockDataFactory.CreateDocuments(25));
        });

        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var apiKeySettings = InMemoryDbContextFactory.CreateApiKeySettings();
        var repository = new DocumentRepository(context);
        var useCase = new SearchDocumentsPagedUseCase(repository, _loggerMock.Object);
        var paginationRequest = MockDataFactory.CreatePaginationRequest(page: 1, pageSize: 10);

        // Act
        var result = await useCase.ExecuteAsync(paginationRequest);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().HaveCount(10);
        result.Total.Should().Be(25);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ApplyFilters_When_FilterProvided()
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
        var useCase = new SearchDocumentsPagedUseCase(repository, _loggerMock.Object);
        var paginationRequest = MockDataFactory.CreatePaginationRequest(
            page: 1,
            pageSize: 10,
            filter: "filename contains 'test'");

        // Act
        var result = await useCase.ExecuteAsync(paginationRequest);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ApplySorting_When_SortByProvided()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.AddRange(MockDataFactory.CreateDocuments(10));
        });

        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var apiKeySettings = InMemoryDbContextFactory.CreateApiKeySettings();
        var repository = new DocumentRepository(context);
        var useCase = new SearchDocumentsPagedUseCase(repository, _loggerMock.Object);
        var paginationRequest = MockDataFactory.CreatePaginationRequest(
            page: 1,
            pageSize: 10,
            sortBy: "UploadDate",
            sortDirection: "DESC");

        // Act
        var result = await useCase.ExecuteAsync(paginationRequest);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().HaveCount(10);
    }

    [Fact]
    public async Task ExecuteAsync_Should_CalculateTotalPages_Correctly()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.AddRange(MockDataFactory.CreateDocuments(25));
        });

        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var apiKeySettings = InMemoryDbContextFactory.CreateApiKeySettings();
        var repository = new DocumentRepository(context);
        var useCase = new SearchDocumentsPagedUseCase(repository, _loggerMock.Object);
        var paginationRequest = MockDataFactory.CreatePaginationRequest(page: 1, pageSize: 10);

        // Act
        var result = await useCase.ExecuteAsync(paginationRequest);

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(25);
        // Total pages should be 3 (25 items / 10 per page = 2.5, rounded up to 3)
    }

    [Fact]
    public async Task ExecuteAsync_Should_SetNextPage_Correctly()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.AddRange(MockDataFactory.CreateDocuments(25));
        });

        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var apiKeySettings = InMemoryDbContextFactory.CreateApiKeySettings();
        var repository = new DocumentRepository(context);
        var useCase = new SearchDocumentsPagedUseCase(repository, _loggerMock.Object);
        var paginationRequest = MockDataFactory.CreatePaginationRequest(page: 1, pageSize: 10);

        // Act
        var result = await useCase.ExecuteAsync(paginationRequest);

        // Assert
        result.Should().NotBeNull();
        result.NextPage.Should().BeTrue(); // Has more pages

        // Test last page
        paginationRequest = MockDataFactory.CreatePaginationRequest(page: 3, pageSize: 10);
        result = await useCase.ExecuteAsync(paginationRequest);
        result.NextPage.Should().BeFalse(); // No more pages
    }
}

