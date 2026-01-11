using FluentAssertions;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Infrastructure.Persistence;
using MicroDocuments.Tests.TestHelpers;
using Xunit;

namespace MicroDocuments.Tests.Infrastructure.Persistence;

public class ApiKeyRepositoryTests
{
    [Fact]
    public async Task GetByKeyHashAsync_Should_ReturnApiKey_When_ExistsAndActive()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.Create();
        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var repository = new ApiKeyRepository(context, httpContextAccessor.Object);
        
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = "Test Key",
            KeyHash = "test-hash",
            IsActive = true,
            RateLimitPerMinute = 100,
            Created = DateTime.UtcNow
        };
        
        context.ApiKeys.Add(apiKey);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByKeyHashAsync("test-hash");

        // Assert
        result.Should().NotBeNull();
        result!.KeyHash.Should().Be("test-hash");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByKeyHashAsync_Should_ReturnNull_When_NotExists()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.Create();
        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var repository = new ApiKeyRepository(context, httpContextAccessor.Object);

        // Act
        var result = await repository.GetByKeyHashAsync("non-existent-hash");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByKeyHashAsync_Should_ReturnNull_When_Inactive()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.Create();
        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var repository = new ApiKeyRepository(context, httpContextAccessor.Object);
        
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Key",
            KeyHash = "inactive-hash",
            IsActive = false,
            RateLimitPerMinute = 100,
            Created = DateTime.UtcNow
        };
        
        context.ApiKeys.Add(apiKey);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByKeyHashAsync("inactive-hash");

        // Assert
        result.Should().BeNull(); // Query filter excludes inactive keys
    }

    [Fact]
    public async Task GetByKeyHashAsync_Should_ReturnNull_When_Expired()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.Create();
        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var repository = new ApiKeyRepository(context, httpContextAccessor.Object);
        
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = "Expired Key",
            KeyHash = "expired-hash",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            RateLimitPerMinute = 100,
            Created = DateTime.UtcNow
        };
        
        context.ApiKeys.Add(apiKey);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByKeyHashAsync("expired-hash");

        // Assert
        result.Should().BeNull(); // Query excludes expired keys
    }

    [Fact]
    public async Task SaveAsync_Should_CreateNewApiKey_When_IdIsEmpty()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.Create();
        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var repository = new ApiKeyRepository(context, httpContextAccessor.Object);
        
        var apiKey = new ApiKey
        {
            Id = Guid.Empty,
            Name = "New Key",
            KeyHash = "new-hash",
            IsActive = true,
            RateLimitPerMinute = 100
        };

        // Act
        var result = await repository.SaveAsync(apiKey);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Created.Should().NotBe(default);
        context.ApiKeys.Count().Should().Be(1);
    }

    [Fact]
    public async Task SaveAsync_Should_UpdateExistingApiKey_When_IdExists()
    {
        // Arrange
        var apiKeyId = Guid.NewGuid();
        using var context = InMemoryDbContextFactory.Create();
        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var repository = new ApiKeyRepository(context, httpContextAccessor.Object);
        
        var existingApiKey = new ApiKey
        {
            Id = apiKeyId,
            Name = "Original Name",
            KeyHash = "original-hash",
            IsActive = true,
            RateLimitPerMinute = 100,
            Created = DateTime.UtcNow
        };
        
        context.ApiKeys.Add(existingApiKey);
        await context.SaveChangesAsync();

        var updatedApiKey = new ApiKey
        {
            Id = apiKeyId,
            Name = "Updated Name",
            KeyHash = "updated-hash",
            IsActive = false,
            RateLimitPerMinute = 200,
            Created = existingApiKey.Created
        };

        // Act
        var result = await repository.SaveAsync(updatedApiKey);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        result.Updated.Should().NotBeNull();
        context.ApiKeys.Count().Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnApiKey_When_Exists()
    {
        // Arrange
        var apiKeyId = Guid.NewGuid();
        using var context = InMemoryDbContextFactory.Create();
        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var repository = new ApiKeyRepository(context, httpContextAccessor.Object);
        
        var apiKey = new ApiKey
        {
            Id = apiKeyId,
            Name = "Test Key",
            KeyHash = "test-hash",
            IsActive = true,
            RateLimitPerMinute = 100,
            Created = DateTime.UtcNow
        };
        
        context.ApiKeys.Add(apiKey);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(apiKeyId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(apiKeyId);
        result.Name.Should().Be("Test Key");
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnNull_When_NotExists()
    {
        // Arrange
        using var context = InMemoryDbContextFactory.Create();
        var httpContextAccessor = InMemoryDbContextFactory.CreateHttpContextAccessor();
        var repository = new ApiKeyRepository(context, httpContextAccessor.Object);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }
}

