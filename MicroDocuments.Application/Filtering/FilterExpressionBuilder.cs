using System.Linq.Expressions;
using System.Reflection;
using MicroDocuments.Application.DTOs;
using MicroDocuments.Application.Filtering.Enums;

namespace MicroDocuments.Application.Filtering;

public static class FilterExpressionBuilder
{
    public static Expression<Func<T, bool>> BuildFilterExpression<T>(List<FilterCriteria> filters)
    {
        if (filters == null || filters.Count == 0)
            return x => true;

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;

        foreach (var filter in filters)
        {
            var expression = BuildSingleFilterExpression<T>(parameter, filter);
            
            if (combinedExpression == null)
            {
                combinedExpression = expression;
            }
            else
            {
                var logicalOp = filter.LogicalOperator ?? LogicalOperator.And;
                combinedExpression = logicalOp == LogicalOperator.And
                    ? Expression.AndAlso(combinedExpression, expression)
                    : Expression.OrElse(combinedExpression, expression);
            }
        }

        return Expression.Lambda<Func<T, bool>>(combinedExpression!, parameter);
    }

    private static Expression BuildSingleFilterExpression<T>(ParameterExpression parameter, FilterCriteria filter)
    {
        var property = GetPropertyInfo<T>(filter.Property);
        if (property == null)
            throw new ArgumentException($"Property '{filter.Property}' not found on type {typeof(T).Name}");

        var propertyAccess = Expression.Property(parameter, property);
        var propertyType = property.PropertyType;

        return filter.Operator switch
        {
            FilterOperator.Equals => BuildEqualsExpression(propertyAccess, filter.Value, propertyType),
            FilterOperator.NotEquals => BuildNotEqualsExpression(propertyAccess, filter.Value, propertyType),
            FilterOperator.GreaterThan => BuildGreaterThanExpression(propertyAccess, filter.Value, propertyType),
            FilterOperator.GreaterThanOrEqual => BuildGreaterThanOrEqualExpression(propertyAccess, filter.Value, propertyType),
            FilterOperator.LessThan => BuildLessThanExpression(propertyAccess, filter.Value, propertyType),
            FilterOperator.LessThanOrEqual => BuildLessThanOrEqualExpression(propertyAccess, filter.Value, propertyType),
            FilterOperator.Contains => BuildContainsExpression(propertyAccess, filter.Value, propertyType),
            FilterOperator.StartsWith => BuildStartsWithExpression(propertyAccess, filter.Value, propertyType),
            FilterOperator.EndsWith => BuildEndsWithExpression(propertyAccess, filter.Value, propertyType),
            FilterOperator.In => BuildInExpression(propertyAccess, filter.Value, propertyType),
            FilterOperator.IsNull => Expression.Equal(propertyAccess, Expression.Constant(null)),
            FilterOperator.IsNotNull => Expression.NotEqual(propertyAccess, Expression.Constant(null)),
            _ => throw new ArgumentException($"Unsupported operator: {filter.Operator}")
        };
    }

    private static PropertyInfo? GetPropertyInfo<T>(string propertyName)
    {
        return typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
    }

    private static Expression BuildEqualsExpression(Expression propertyAccess, object? value, Type propertyType)
    {
        var convertedValue = ConvertValue(value, propertyType);
        return Expression.Equal(propertyAccess, Expression.Constant(convertedValue, propertyType));
    }

    private static Expression BuildNotEqualsExpression(Expression propertyAccess, object? value, Type propertyType)
    {
        var convertedValue = ConvertValue(value, propertyType);
        return Expression.NotEqual(propertyAccess, Expression.Constant(convertedValue, propertyType));
    }

    private static Expression BuildGreaterThanExpression(Expression propertyAccess, object? value, Type propertyType)
    {
        var convertedValue = ConvertValue(value, propertyType);
        return Expression.GreaterThan(propertyAccess, Expression.Constant(convertedValue, propertyType));
    }

    private static Expression BuildGreaterThanOrEqualExpression(Expression propertyAccess, object? value, Type propertyType)
    {
        var convertedValue = ConvertValue(value, propertyType);
        return Expression.GreaterThanOrEqual(propertyAccess, Expression.Constant(convertedValue, propertyType));
    }

    private static Expression BuildLessThanExpression(Expression propertyAccess, object? value, Type propertyType)
    {
        var convertedValue = ConvertValue(value, propertyType);
        return Expression.LessThan(propertyAccess, Expression.Constant(convertedValue, propertyType));
    }

    private static Expression BuildLessThanOrEqualExpression(Expression propertyAccess, object? value, Type propertyType)
    {
        var convertedValue = ConvertValue(value, propertyType);
        return Expression.LessThanOrEqual(propertyAccess, Expression.Constant(convertedValue, propertyType));
    }

    private static Expression BuildContainsExpression(Expression propertyAccess, object? value, Type propertyType)
    {
        if (propertyType != typeof(string))
            throw new ArgumentException("Contains operator can only be used with string properties");

        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        var valueExpression = Expression.Constant(value?.ToString(), typeof(string));
        return Expression.Call(propertyAccess, containsMethod!, valueExpression);
    }

    private static Expression BuildStartsWithExpression(Expression propertyAccess, object? value, Type propertyType)
    {
        if (propertyType != typeof(string))
            throw new ArgumentException("StartsWith operator can only be used with string properties");

        var startsWithMethod = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
        var valueExpression = Expression.Constant(value?.ToString(), typeof(string));
        return Expression.Call(propertyAccess, startsWithMethod!, valueExpression);
    }

    private static Expression BuildEndsWithExpression(Expression propertyAccess, object? value, Type propertyType)
    {
        if (propertyType != typeof(string))
            throw new ArgumentException("EndsWith operator can only be used with string properties");

        var endsWithMethod = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
        var valueExpression = Expression.Constant(value?.ToString(), typeof(string));
        return Expression.Call(propertyAccess, endsWithMethod!, valueExpression);
    }

    private static Expression BuildInExpression(Expression propertyAccess, object? value, Type propertyType)
    {
        if (value is not List<string> list || list.Count == 0)
            throw new ArgumentException("In operator requires a list of values");

        var convertedValues = list.Select(v => ConvertValue(v, propertyType)).ToList();
        
        Expression? inExpression = null;
        foreach (var convertedValue in convertedValues)
        {
            var equalsExpression = Expression.Equal(
                propertyAccess,
                Expression.Constant(convertedValue, propertyType));

            if (inExpression == null)
            {
                inExpression = equalsExpression;
            }
            else
            {
                inExpression = Expression.OrElse(inExpression, equalsExpression);
            }
        }

        return inExpression ?? Expression.Constant(false);
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
            return null;

        if (targetType.IsEnum)
        {
            if (value is string strValue)
                return Enum.Parse(targetType, strValue, ignoreCase: true);
            return Enum.ToObject(targetType, value);
        }

        if (targetType == typeof(Guid))
        {
            if (value is string strGuid)
                return Guid.Parse(strGuid);
            return value;
        }

        if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
        {
            if (value is string strDate)
                return DateTime.Parse(strDate);
            return value;
        }

        if (targetType == typeof(bool) || targetType == typeof(bool?))
        {
            if (value is string strBool)
                return bool.Parse(strBool);
            return value;
        }

        return Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType) ?? targetType);
    }
}

