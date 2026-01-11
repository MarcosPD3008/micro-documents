using FluentAssertions;
using MicroDocuments.Application.DTOs;
using MicroDocuments.Application.Filtering;
using MicroDocuments.Application.Filtering.Enums;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Enums;
using MicroDocuments.Tests.TestHelpers;
using Xunit;

namespace MicroDocuments.Tests.Application.Filtering;

public class FilterExpressionBuilderTests
{
    [Fact]
    public void BuildFilterExpression_Should_ReturnTrue_When_NoFilters()
    {
        // Arrange
        var filters = new List<FilterCriteria>();

        // Act
        var expression = FilterExpressionBuilder.BuildFilterExpression<Document>(filters);

        // Assert
        expression.Should().NotBeNull();
        var compiled = expression.Compile();
        var document = new DocumentBuilder().Build();
        compiled(document).Should().BeTrue();
    }

    [Fact]
    public void BuildFilterExpression_Should_BuildEqualsExpression()
    {
        // Arrange
        var filters = new List<FilterCriteria>
        {
            new FilterCriteria
            {
                Property = "Filename",
                Operator = FilterOperator.Equals,
                Value = "test.pdf"
            }
        };

        // Act
        var expression = FilterExpressionBuilder.BuildFilterExpression<Document>(filters);

        // Assert
        expression.Should().NotBeNull();
        var compiled = expression.Compile();
        var matchingDoc = new DocumentBuilder().WithFilename("test.pdf").Build();
        var nonMatchingDoc = new DocumentBuilder().WithFilename("other.pdf").Build();
        
        compiled(matchingDoc).Should().BeTrue();
        compiled(nonMatchingDoc).Should().BeFalse();
    }

    [Fact]
    public void BuildFilterExpression_Should_BuildNotEqualsExpression()
    {
        // Arrange
        var filters = new List<FilterCriteria>
        {
            new FilterCriteria
            {
                Property = "Filename",
                Operator = FilterOperator.NotEquals,
                Value = "test.pdf"
            }
        };

        // Act
        var expression = FilterExpressionBuilder.BuildFilterExpression<Document>(filters);

        // Assert
        var compiled = expression.Compile();
        var matchingDoc = new DocumentBuilder().WithFilename("other.pdf").Build();
        var nonMatchingDoc = new DocumentBuilder().WithFilename("test.pdf").Build();
        
        compiled(matchingDoc).Should().BeTrue();
        compiled(nonMatchingDoc).Should().BeFalse();
    }

    [Fact]
    public void BuildFilterExpression_Should_BuildComparisonExpressions()
    {
        // Arrange
        var filters = new List<FilterCriteria>
        {
            new FilterCriteria
            {
                Property = "Size",
                Operator = FilterOperator.GreaterThan,
                Value = "1000"
            }
        };

        // Act
        var expression = FilterExpressionBuilder.BuildFilterExpression<Document>(filters);

        // Assert
        var compiled = expression.Compile();
        var matchingDoc = new DocumentBuilder().WithSize(2000).Build();
        var nonMatchingDoc = new DocumentBuilder().WithSize(500).Build();
        
        compiled(matchingDoc).Should().BeTrue();
        compiled(nonMatchingDoc).Should().BeFalse();
    }

    [Fact]
    public void BuildFilterExpression_Should_BuildStringExpressions()
    {
        // Arrange
        var filters = new List<FilterCriteria>
        {
            new FilterCriteria
            {
                Property = "Filename",
                Operator = FilterOperator.Contains,
                Value = "test"
            }
        };

        // Act
        var expression = FilterExpressionBuilder.BuildFilterExpression<Document>(filters);

        // Assert
        var compiled = expression.Compile();
        var matchingDoc = new DocumentBuilder().WithFilename("test-file.pdf").Build();
        var nonMatchingDoc = new DocumentBuilder().WithFilename("other.pdf").Build();
        
        compiled(matchingDoc).Should().BeTrue();
        compiled(nonMatchingDoc).Should().BeFalse();
    }

    [Fact]
    public void BuildFilterExpression_Should_BuildInExpression()
    {
        // Arrange
        var filters = new List<FilterCriteria>
        {
            new FilterCriteria
            {
                Property = "Status",
                Operator = FilterOperator.In,
                Value = new List<string> { "RECEIVED", "SENT" }
            }
        };

        // Act
        var expression = FilterExpressionBuilder.BuildFilterExpression<Document>(filters);

        // Assert
        var compiled = expression.Compile();
        var matchingDoc1 = new DocumentBuilder().WithStatus(DocumentStatus.RECEIVED).Build();
        var matchingDoc2 = new DocumentBuilder().WithStatus(DocumentStatus.SENT).Build();
        var nonMatchingDoc = new DocumentBuilder().WithStatus(DocumentStatus.FAILED).Build();
        
        compiled(matchingDoc1).Should().BeTrue();
        compiled(matchingDoc2).Should().BeTrue();
        compiled(nonMatchingDoc).Should().BeFalse();
    }

    [Fact]
    public void BuildFilterExpression_Should_BuildNullExpressions()
    {
        // Arrange
        var filters = new List<FilterCriteria>
        {
            new FilterCriteria
            {
                Property = "CustomerId",
                Operator = FilterOperator.IsNull,
                Value = null
            }
        };

        // Act
        var expression = FilterExpressionBuilder.BuildFilterExpression<Document>(filters);

        // Assert
        var compiled = expression.Compile();
        var matchingDoc = new DocumentBuilder().WithCustomerId(null).Build();
        var nonMatchingDoc = new DocumentBuilder().WithCustomerId("customer-123").Build();
        
        compiled(matchingDoc).Should().BeTrue();
        compiled(nonMatchingDoc).Should().BeFalse();
    }

    [Fact]
    public void BuildFilterExpression_Should_CombineWithAndOperator()
    {
        // Arrange
        var filters = new List<FilterCriteria>
        {
            new FilterCriteria
            {
                Property = "Filename",
                Operator = FilterOperator.Contains,
                Value = "test"
            },
            new FilterCriteria
            {
                Property = "Status",
                Operator = FilterOperator.Equals,
                Value = "RECEIVED",
                LogicalOperator = LogicalOperator.And
            }
        };

        // Act
        var expression = FilterExpressionBuilder.BuildFilterExpression<Document>(filters);

        // Assert
        var compiled = expression.Compile();
        var matchingDoc = new DocumentBuilder()
            .WithFilename("test.pdf")
            .WithStatus(DocumentStatus.RECEIVED)
            .Build();
        var nonMatchingDoc = new DocumentBuilder()
            .WithFilename("test.pdf")
            .WithStatus(DocumentStatus.SENT)
            .Build();
        
        compiled(matchingDoc).Should().BeTrue();
        compiled(nonMatchingDoc).Should().BeFalse();
    }

    [Fact]
    public void BuildFilterExpression_Should_CombineWithOrOperator()
    {
        // Arrange
        var filters = new List<FilterCriteria>
        {
            new FilterCriteria
            {
                Property = "Status",
                Operator = FilterOperator.Equals,
                Value = "RECEIVED"
            },
            new FilterCriteria
            {
                Property = "Status",
                Operator = FilterOperator.Equals,
                Value = "SENT",
                LogicalOperator = LogicalOperator.Or
            }
        };

        // Act
        var expression = FilterExpressionBuilder.BuildFilterExpression<Document>(filters);

        // Assert
        var compiled = expression.Compile();
        var matchingDoc1 = new DocumentBuilder().WithStatus(DocumentStatus.RECEIVED).Build();
        var matchingDoc2 = new DocumentBuilder().WithStatus(DocumentStatus.SENT).Build();
        var nonMatchingDoc = new DocumentBuilder().WithStatus(DocumentStatus.FAILED).Build();
        
        compiled(matchingDoc1).Should().BeTrue();
        compiled(matchingDoc2).Should().BeTrue();
        compiled(nonMatchingDoc).Should().BeFalse();
    }

    [Fact]
    public void BuildFilterExpression_Should_Throw_When_PropertyNotFound()
    {
        // Arrange
        var filters = new List<FilterCriteria>
        {
            new FilterCriteria
            {
                Property = "NonExistentProperty",
                Operator = FilterOperator.Equals,
                Value = "value"
            }
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            FilterExpressionBuilder.BuildFilterExpression<Document>(filters));
    }

    [Fact]
    public void BuildFilterExpression_Should_ConvertEnumValues()
    {
        // Arrange
        var filters = new List<FilterCriteria>
        {
            new FilterCriteria
            {
                Property = "DocumentType",
                Operator = FilterOperator.Equals,
                Value = "CONTRACT"
            }
        };

        // Act
        var expression = FilterExpressionBuilder.BuildFilterExpression<Document>(filters);

        // Assert
        var compiled = expression.Compile();
        var matchingDoc = new DocumentBuilder().WithDocumentType(DocumentType.CONTRACT).Build();
        var nonMatchingDoc = new DocumentBuilder().WithDocumentType(DocumentType.KYC).Build();
        
        compiled(matchingDoc).Should().BeTrue();
        compiled(nonMatchingDoc).Should().BeFalse();
    }

    [Fact]
    public void BuildFilterExpression_Should_ConvertDateTimeValues()
    {
        // Arrange
        var testDate = DateTime.UtcNow;
        var filters = new List<FilterCriteria>
        {
            new FilterCriteria
            {
                Property = "UploadDate",
                Operator = FilterOperator.GreaterThanOrEqual,
                Value = testDate.ToString("yyyy-MM-ddTHH:mm:ss")
            }
        };

        // Act
        var expression = FilterExpressionBuilder.BuildFilterExpression<Document>(filters);

        // Assert
        var compiled = expression.Compile();
        var matchingDoc = new DocumentBuilder().WithUploadDate(testDate.AddDays(1)).Build();
        var nonMatchingDoc = new DocumentBuilder().WithUploadDate(testDate.AddDays(-1)).Build();
        
        compiled(matchingDoc).Should().BeTrue();
        compiled(nonMatchingDoc).Should().BeFalse();
    }

    [Fact]
    public void BuildFilterExpression_Should_ConvertGuidValues()
    {
        // Arrange
        var testGuid = Guid.NewGuid();
        var filters = new List<FilterCriteria>
        {
            new FilterCriteria
            {
                Property = "Id",
                Operator = FilterOperator.Equals,
                Value = testGuid.ToString()
            }
        };

        // Act
        var expression = FilterExpressionBuilder.BuildFilterExpression<Document>(filters);

        // Assert
        var compiled = expression.Compile();
        var matchingDoc = new DocumentBuilder().WithId(testGuid).Build();
        var nonMatchingDoc = new DocumentBuilder().WithId(Guid.NewGuid()).Build();
        
        compiled(matchingDoc).Should().BeTrue();
        compiled(nonMatchingDoc).Should().BeFalse();
    }
}

