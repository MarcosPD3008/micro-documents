using FluentAssertions;
using MicroDocuments.Application.DTOs;
using MicroDocuments.Application.Filtering;
using MicroDocuments.Application.Filtering.Enums;
using Xunit;

namespace MicroDocuments.Tests.Application.Filtering;

public class FilterParserTests
{
    [Fact]
    public void Parse_Should_ReturnEmptyList_When_FilterStringIsNull()
    {
        // Act
        var result = FilterParser.Parse(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_Should_ReturnEmptyList_When_FilterStringIsEmpty()
    {
        // Act
        var result = FilterParser.Parse(string.Empty);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_Should_ParseSingleFilter_WithEqualsOperator()
    {
        // Arrange
        var filterString = "filename eq 'test.pdf'";

        // Act
        var result = FilterParser.Parse(filterString);

        // Assert
        result.Should().HaveCount(1);
        result[0].Property.Should().Be("filename");
        result[0].Operator.Should().Be(FilterOperator.Equals);
        result[0].Value.Should().Be("test.pdf");
    }

    [Fact]
    public void Parse_Should_ParseMultipleFilters_WithAndOperator()
    {
        // Arrange
        var filterString = "filename eq 'test.pdf' and status eq 'RECEIVED'";

        // Act
        var result = FilterParser.Parse(filterString);

        // Assert
        result.Should().HaveCount(2);
        result[0].Property.Should().Be("filename");
        result[0].LogicalOperator.Should().BeNull(); // El primer filtro no tiene operador lógico
        result[1].Property.Should().Be("status");
        result[1].LogicalOperator.Should().Be(LogicalOperator.And); // El segundo filtro tiene el operador
    }

    [Fact]
    public void Parse_Should_ParseMultipleFilters_WithOrOperator()
    {
        // Arrange
        var filterString = "status eq 'RECEIVED' or status eq 'SENT'";

        // Act
        var result = FilterParser.Parse(filterString);

        // Assert
        result.Should().HaveCount(2);
        result[0].Property.Should().Be("status");
        result[1].Property.Should().Be("status");
        result[1].LogicalOperator.Should().Be(LogicalOperator.Or);
    }

    [Fact]
    public void Parse_Should_ParseDateFilters()
    {
        // Arrange
        var filterString = "uploadDate ge '2024-01-01T00:00:00'";

        // Act
        var result = FilterParser.Parse(filterString);

        // Assert
        result.Should().HaveCount(1);
        result[0].Property.Should().Be("uploadDate");
        result[0].Operator.Should().Be(FilterOperator.GreaterThanOrEqual);
        result[0].Value.Should().Be("2024-01-01T00:00:00");
    }

    [Fact]
    public void Parse_Should_ParseStringFilters_WithQuotes()
    {
        // Arrange
        var filterString = "filename eq 'test file.pdf'";

        // Act
        var result = FilterParser.Parse(filterString);

        // Assert
        result.Should().HaveCount(1);
        result[0].Value.Should().Be("test file.pdf");
    }

    [Fact]
    public void Parse_Should_ParseInOperator_WithList()
    {
        // Arrange
        var filterString = "status in ('RECEIVED', 'SENT')";

        // Act
        var result = FilterParser.Parse(filterString);

        // Assert
        result.Should().HaveCount(1);
        result[0].Operator.Should().Be(FilterOperator.In);
        result[0].Value.Should().BeOfType<List<string>>();
        var list = result[0].Value as List<string>;
        list.Should().Contain("RECEIVED");
        list.Should().Contain("SENT");
    }

    [Fact]
    public void Parse_Should_ParseIsNull_IsNotNull()
    {
        // Arrange
        var filterString = "customerId isnull";

        // Act
        var result = FilterParser.Parse(filterString);

        // Assert
        result.Should().HaveCount(1);
        result[0].Property.Should().Be("customerId");
        result[0].Operator.Should().Be(FilterOperator.IsNull);
        result[0].Value.Should().BeNull();
    }

    [Fact]
    public void Parse_Should_HandleUrlEncodedStrings()
    {
        // Arrange
        var filterString = "filename%20eq%20'test%20file.pdf'";

        // Act
        var result = FilterParser.Parse(filterString);

        // Assert
        result.Should().HaveCount(1);
        result[0].Property.Should().Be("filename");
        result[0].Value.Should().Be("test file.pdf");
    }

    [Theory]
    [InlineData("filename eq 'test.pdf'", FilterOperator.Equals)]
    [InlineData("size gt 1000", FilterOperator.GreaterThan)]
    [InlineData("size ge 1000", FilterOperator.GreaterThanOrEqual)]
    [InlineData("size lt 5000", FilterOperator.LessThan)]
    [InlineData("size le 5000", FilterOperator.LessThanOrEqual)]
    [InlineData("size lte 5000", FilterOperator.LessThanOrEqual)]
    [InlineData("filename contains 'test'", FilterOperator.Contains)]
    [InlineData("filename startswith 'test'", FilterOperator.StartsWith)]
    [InlineData("filename endswith '.pdf'", FilterOperator.EndsWith)]
    public void Parse_Should_ParseAllOperators(string filterString, FilterOperator expectedOperator)
    {
        // Act
        var result = FilterParser.Parse(filterString);

        // Assert
        result.Should().HaveCount(1);
        result[0].Operator.Should().Be(expectedOperator);
    }
}


