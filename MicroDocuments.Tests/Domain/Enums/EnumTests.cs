using FluentAssertions;
using MicroDocuments.Domain.Enums;
using System.Text.Json;

namespace MicroDocuments.Tests.Domain.Enums;

public class EnumTests
{
    [Theory]
    [InlineData(DocumentType.KYC)]
    [InlineData(DocumentType.CONTRACT)]
    [InlineData(DocumentType.FORM)]
    [InlineData(DocumentType.SUPPORTING_DOCUMENT)]
    [InlineData(DocumentType.OTHER)]
    public void DocumentType_Should_HaveValidValues(DocumentType documentType)
    {
        // Act
        var enumValue = (int)documentType;

        // Assert
        enumValue.Should().BeGreaterThanOrEqualTo(0);
        Enum.IsDefined(typeof(DocumentType), documentType).Should().BeTrue();
    }

    [Theory]
    [InlineData(Channel.BRANCH)]
    [InlineData(Channel.DIGITAL)]
    [InlineData(Channel.BACKOFFICE)]
    [InlineData(Channel.OTHER)]
    public void Channel_Should_HaveValidValues(Channel channel)
    {
        // Act
        var enumValue = (int)channel;

        // Assert
        enumValue.Should().BeGreaterThanOrEqualTo(0);
        Enum.IsDefined(typeof(Channel), channel).Should().BeTrue();
    }

    [Theory]
    [InlineData(DocumentStatus.RECEIVED)]
    [InlineData(DocumentStatus.SENT)]
    [InlineData(DocumentStatus.FAILED)]
    public void DocumentStatus_Should_HaveValidValues(DocumentStatus status)
    {
        // Act
        var enumValue = (int)status;

        // Assert
        enumValue.Should().BeGreaterThanOrEqualTo(0);
        Enum.IsDefined(typeof(DocumentStatus), status).Should().BeTrue();
    }

    [Fact]
    public void DocumentType_Should_SerializeAndDeserialize()
    {
        // Arrange
        var original = DocumentType.CONTRACT;

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<DocumentType>(json);

        // Assert
        deserialized.Should().Be(original);
    }

    [Fact]
    public void Channel_Should_SerializeAndDeserialize()
    {
        // Arrange
        var original = Channel.DIGITAL;

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Channel>(json);

        // Assert
        deserialized.Should().Be(original);
    }

    [Fact]
    public void DocumentStatus_Should_SerializeAndDeserialize()
    {
        // Arrange
        var original = DocumentStatus.SENT;

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<DocumentStatus>(json);

        // Assert
        deserialized.Should().Be(original);
    }
}






