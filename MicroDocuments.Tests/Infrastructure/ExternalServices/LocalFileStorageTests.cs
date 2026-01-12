using FluentAssertions;
using Microsoft.Extensions.Logging;
using MicroDocuments.Infrastructure.ExternalServices;
using MicroDocuments.Tests.TestHelpers;
using Moq;
using Xunit;

namespace MicroDocuments.Tests.Infrastructure.ExternalServices;

public class LocalFileStorageTests : IDisposable
{
    private readonly string _testStoragePath;
    private readonly Mock<ILogger<LocalFileStorage>> _loggerMock;
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        _testStoragePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _loggerMock = new Mock<ILogger<LocalFileStorage>>();
        
        // Create storage with custom path by using reflection or creating a test-specific implementation
        // For simplicity, we'll use the default implementation and clean up after
        _storage = new LocalFileStorage(_loggerMock.Object);
    }

    [Fact]
    public async Task SaveAsync_Should_SaveFile_When_ValidContent()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var content = MockDataFactory.CreateTestFileContent(1024);

        // Act
        await _storage.SaveAsync(documentId, content);

        // Assert
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "temp_uploads", $"{documentId}.tmp");
        File.Exists(filePath).Should().BeTrue();
        var savedContent = await File.ReadAllBytesAsync(filePath);
        savedContent.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task SaveAsync_Should_CreateDirectory_When_NotExists()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var content = new byte[] { 1, 2, 3, 4 };

        // Act
        await _storage.SaveAsync(documentId, content);

        // Assert
        var storagePath = Path.Combine(Directory.GetCurrentDirectory(), "temp_uploads");
        Directory.Exists(storagePath).Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_Should_ReturnFileContent_When_FileExists()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var content = MockDataFactory.CreateTestFileContent(512);
        await _storage.SaveAsync(documentId, content);

        // Act
        var result = await _storage.GetAsync(documentId);

        // Assert
        result.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task GetAsync_Should_Throw_When_FileNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => _storage.GetAsync(documentId));
    }

    [Fact]
    public async Task DeleteAsync_Should_DeleteFile_When_Exists()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var content = new byte[] { 1, 2, 3, 4 };
        await _storage.SaveAsync(documentId, content);

        // Act
        await _storage.DeleteAsync(documentId);

        // Assert
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "temp_uploads", $"{documentId}.tmp");
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_Should_NotThrow_When_FileNotExists()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act & Assert
        await _storage.DeleteAsync(documentId); // Should not throw
    }

    [Fact]
    public async Task SaveFromStreamAsync_Should_SaveStream_When_ValidStream()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var stream = MockDataFactory.CreateTestStream(1024);
        var expectedContent = new byte[1024];
        stream.Read(expectedContent, 0, 1024);
        stream.Position = 0;

        // Act
        await _storage.SaveFromStreamAsync(documentId, stream);

        // Assert
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "temp_uploads", $"{documentId}.tmp");
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task GetStreamAsync_Should_ReturnStream_When_FileExists()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var content = MockDataFactory.CreateTestFileContent(512);
        await _storage.SaveAsync(documentId, content);

        // Act
        var result = await _storage.GetStreamAsync(documentId);

        // Assert
        result.Should().NotBeNull();
        result.CanRead.Should().BeTrue();
        result.Dispose();
    }

    [Fact]
    public async Task GetStreamAsync_Should_Throw_When_FileNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => _storage.GetStreamAsync(documentId));
    }

    [Fact]
    public async Task CleanupOrphanedFilesAsync_Should_DeleteOrphanedFiles()
    {
        // Arrange
        var validId = Guid.NewGuid();
        var orphanedId = Guid.NewGuid();
        await _storage.SaveAsync(validId, new byte[] { 1 });
        await _storage.SaveAsync(orphanedId, new byte[] { 2 });
        var validIds = new[] { validId };

        // Act
        await _storage.CleanupOrphanedFilesAsync(validIds);

        // Assert
        var validFilePath = Path.Combine(Directory.GetCurrentDirectory(), "temp_uploads", $"{validId}.tmp");
        var orphanedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "temp_uploads", $"{orphanedId}.tmp");
        File.Exists(validFilePath).Should().BeTrue();
        File.Exists(orphanedFilePath).Should().BeFalse();
    }

    [Fact]
    public async Task CleanupOrphanedFilesAsync_Should_KeepValidFiles()
    {
        // Arrange
        var validId1 = Guid.NewGuid();
        var validId2 = Guid.NewGuid();
        await _storage.SaveAsync(validId1, new byte[] { 1 });
        await _storage.SaveAsync(validId2, new byte[] { 2 });
        var validIds = new[] { validId1, validId2 };

        // Act
        await _storage.CleanupOrphanedFilesAsync(validIds);

        // Assert
        var filePath1 = Path.Combine(Directory.GetCurrentDirectory(), "temp_uploads", $"{validId1}.tmp");
        var filePath2 = Path.Combine(Directory.GetCurrentDirectory(), "temp_uploads", $"{validId2}.tmp");
        File.Exists(filePath1).Should().BeTrue();
        File.Exists(filePath2).Should().BeTrue();
    }

    public void Dispose()
    {
        // Clean up test files
        var storagePath = Path.Combine(Directory.GetCurrentDirectory(), "temp_uploads");
        if (Directory.Exists(storagePath))
        {
            try
            {
                var files = Directory.GetFiles(storagePath, "*.tmp");
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}





