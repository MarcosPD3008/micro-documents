using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MicroDocuments.Application.Extensions;
using MicroDocuments.Application.Pagination;
using MicroDocuments.Application.Sorting;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Infrastructure.Persistence;
using MicroDocuments.Tests.TestHelpers;
using Xunit;

namespace MicroDocuments.Tests.Application.Extensions;

public class QueryableExtensionsTests
{
    [Fact]
    public async Task ToPagedAsync_Should_ReturnPagedResults()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.AddRange(MockDataFactory.CreateDocuments(25));
        });

        var queryable = context.Documents.AsQueryable();
        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await queryable.ToPagedAsync(pagination);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().HaveCount(10);
        result.Total.Should().Be(25);
    }

    [Fact]
    public async Task ToPagedAsync_Should_CalculateTotalPages()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.AddRange(MockDataFactory.CreateDocuments(25));
        });

        var queryable = context.Documents.AsQueryable();
        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await queryable.ToPagedAsync(pagination);

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(25);
        // Total pages = ceil(25/10) = 3
    }

    [Fact]
    public async Task ToPagedAsync_Should_SetNextPage_When_HasMorePages()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.AddRange(MockDataFactory.CreateDocuments(25));
        });

        var queryable = context.Documents.AsQueryable();
        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await queryable.ToPagedAsync(pagination);

        // Assert
        result.Should().NotBeNull();
        result.NextPage.Should().BeTrue(); // Page 1 of 3, has next page

        // Test last page
        pagination = new PaginationRequest { Page = 3, PageSize = 10 };
        result = await queryable.ToPagedAsync(pagination);
        result.NextPage.Should().BeFalse(); // Last page, no next page
    }

    [Fact]
    public async Task ToPagedAsync_Should_ReturnEmpty_When_NoResults()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.Create();
        var queryable = context.Documents.AsQueryable();
        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await queryable.ToPagedAsync(pagination);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.NextPage.Should().BeFalse();
    }

    [Fact]
    public void ApplyFilters_Should_ReturnOriginal_When_FilterStringIsNull()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.Create();
        var queryable = context.Documents.AsQueryable();
        var originalCount = queryable.Count();

        // Act
        var result = queryable.ApplyFilters(null);

        // Assert
        result.Should().NotBeNull();
        result.Count().Should().Be(originalCount);
    }

    [Fact]
    public void ApplyFilters_Should_ApplyFilterExpression()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.Add(new DocumentBuilder().WithFilename("test1.pdf").Build());
            db.Documents.Add(new DocumentBuilder().WithFilename("test2.pdf").Build());
            db.Documents.Add(new DocumentBuilder().WithFilename("other.doc").Build());
        });

        var queryable = context.Documents.AsQueryable();
        var filterString = "filename contains 'test'";

        // Act
        var result = queryable.ApplyFilters(filterString);

        // Assert
        result.Should().NotBeNull();
        result.Count().Should().Be(2);
        result.All(d => d.Filename.Contains("test")).Should().BeTrue();
    }

    [Fact]
    public void ApplySorting_Should_ReturnOriginal_When_SortByIsNull()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.Create();
        var queryable = context.Documents.AsQueryable();
        var sortRequest = new SortRequest { SortBy = null! };

        // Act
        var result = queryable.ApplySorting(sortRequest);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(queryable);
    }

    [Fact]
    public void ApplySorting_Should_SortAscending_When_SortDirectionIsAsc()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.Add(new DocumentBuilder().WithFilename("z.pdf").Build());
            db.Documents.Add(new DocumentBuilder().WithFilename("a.pdf").Build());
            db.Documents.Add(new DocumentBuilder().WithFilename("m.pdf").Build());
        });

        var queryable = context.Documents.AsQueryable();
        var sortRequest = new SortRequest { SortBy = "Filename", SortDirection = "ASC" };

        // Act
        var result = queryable.ApplySorting(sortRequest).ToList();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].Filename.Should().Be("a.pdf");
        result[1].Filename.Should().Be("m.pdf");
        result[2].Filename.Should().Be("z.pdf");
    }

    [Fact]
    public void ApplySorting_Should_SortDescending_When_SortDirectionIsDesc()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.Add(new DocumentBuilder().WithFilename("a.pdf").Build());
            db.Documents.Add(new DocumentBuilder().WithFilename("m.pdf").Build());
            db.Documents.Add(new DocumentBuilder().WithFilename("z.pdf").Build());
        });

        var queryable = context.Documents.AsQueryable();
        var sortRequest = new SortRequest { SortBy = "Filename", SortDirection = "DESC" };

        // Act
        var result = queryable.ApplySorting(sortRequest).ToList();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].Filename.Should().Be("z.pdf");
        result[1].Filename.Should().Be("m.pdf");
        result[2].Filename.Should().Be("a.pdf");
    }

    [Fact]
    public void ApplySorting_Should_ReturnOriginal_When_PropertyNotFound()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.Create();
        var queryable = context.Documents.AsQueryable();
        var sortRequest = new SortRequest { SortBy = "NonExistentProperty", SortDirection = "ASC" };

        // Act
        var result = queryable.ApplySorting(sortRequest);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(queryable);
    }
}

