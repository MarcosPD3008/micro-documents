using FluentAssertions;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Enums;
using MicroDocuments.Infrastructure.Persistence;
using MicroDocuments.Tests.TestHelpers;
using Xunit;

namespace MicroDocuments.Tests.Infrastructure.Persistence;

public class DocumentRepositoryTests
{
    [Fact]
    public async Task CreateAsync_Should_AddNewDocument_When_IdIsEmpty()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.Create();
        var repository = new DocumentRepository(context);
        var document = new DocumentBuilder()
            .WithId(Guid.Empty)
            .Build();

        // Act
        var result = await repository.CreateAsync(document);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Id.Should().NotBe(Guid.Empty);
        context.Documents.Count().Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_Should_UpdateExistingDocument_When_IdExists()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.Add(new DocumentBuilder()
                .WithId(documentId)
                .WithFilename("old.pdf")
                .Build());
        });

        var repository = new DocumentRepository(context);
        var updatedDocument = new DocumentBuilder()
            .WithId(documentId)
            .WithFilename("new.pdf")
            .Build();

        // Act
        var result = await repository.UpdateAsync(updatedDocument);

        // Assert
        result.Should().NotBeNull();
        result.Filename.Should().Be("new.pdf");
        context.Documents.Count().Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_Should_SetCreated_When_NewDocument()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.Create();
        var repository = new DocumentRepository(context);
        var document = new DocumentBuilder()
            .WithId(Guid.Empty)
            .Build();
        document.Created = default;

        // Act
        var result = await repository.CreateAsync(document);

        // Assert
        result.Created.Should().NotBe(default);
        result.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateAsync_Should_SetUpdated_When_ExistingDocument()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.Add(new DocumentBuilder()
                .WithId(documentId)
                .Build());
        });

        var repository = new DocumentRepository(context);
        var updatedDocument = new DocumentBuilder()
            .WithId(documentId)
            .Build();
        updatedDocument.Updated = null;

        // Act
        var result = await repository.UpdateAsync(updatedDocument);

        // Assert
        result.Updated.Should().NotBeNull();
        result.Updated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnDocument_When_Exists()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.Add(new DocumentBuilder()
                .WithId(documentId)
                .WithFilename("test.pdf")
                .Build());
        });

        var repository = new DocumentRepository(context);

        // Act
        var result = await repository.GetByIdAsync(documentId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(documentId);
        result.Filename.Should().Be("test.pdf");
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnNull_When_NotExists()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.Create();
        var repository = new DocumentRepository(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetAll_Should_ReturnQueryable()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.CreateWithSeed(db =>
        {
            db.Documents.AddRange(MockDataFactory.CreateDocuments(5));
        });

        var repository = new DocumentRepository(context);

        // Act
        var result = repository.GetAll();

        // Assert
        result.Should().NotBeNull();
        result.Count().Should().Be(5);
    }
}

