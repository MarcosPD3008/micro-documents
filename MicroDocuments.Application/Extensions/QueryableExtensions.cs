using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MicroDocuments.Application.Filtering;
using MicroDocuments.Application.Pagination;
using MicroDocuments.Application.Sorting;

namespace MicroDocuments.Application.Extensions;

public static class QueryableExtensions
{
    public static async Task<PaginationResponse<T>> ToPagedAsync<T>(
        this IQueryable<T> queryable,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var total = await queryable.CountAsync(cancellationToken);
        
        var skip = (pagination.Page - 1) * pagination.PageSize;
        var items = await queryable
            .Skip(skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(total / (double)pagination.PageSize);
        var hasNextPage = pagination.Page < totalPages;

        return new PaginationResponse<T>
        {
            Content = items,
            Total = total,
            NextPage = hasNextPage
        };
    }

    public static IQueryable<T> ApplyFilters<T>(
        this IQueryable<T> queryable,
        string? filterString)
    {
        if (string.IsNullOrWhiteSpace(filterString))
            return queryable;

        var filters = FilterParser.Parse(filterString);
        if (filters.Count == 0)
            return queryable;

        var filterExpression = FilterExpressionBuilder.BuildFilterExpression<T>(filters);
        return queryable.Where(filterExpression);
    }

    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> queryable,
        SortRequest sortRequest)
    {
        if (string.IsNullOrWhiteSpace(sortRequest.SortBy))
            return queryable;

        var propertyInfo = typeof(T).GetProperty(
            sortRequest.SortBy,
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (propertyInfo == null)
            return queryable;

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, propertyInfo);
        var lambda = Expression.Lambda(property, parameter);

        var methodName = sortRequest.SortDirection.ToUpper() == "DESC" 
            ? "OrderByDescending" 
            : "OrderBy";

        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            new Type[] { typeof(T), propertyInfo.PropertyType },
            queryable.Expression,
            Expression.Quote(lambda));

        return queryable.Provider.CreateQuery<T>(resultExpression);
    }
}

