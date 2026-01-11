using System.Text;
using System.Text.RegularExpressions;
using MicroDocuments.Application.DTOs;
using MicroDocuments.Application.Filtering.Enums;

namespace MicroDocuments.Application.Filtering;

public class FilterParser
{
    private static readonly Dictionary<string, FilterOperator> OperatorMap = new()
    {
        { "eq", FilterOperator.Equals },
        { "ne", FilterOperator.NotEquals },
        { "neq", FilterOperator.NotEquals },
        { "gt", FilterOperator.GreaterThan },
        { "ge", FilterOperator.GreaterThanOrEqual },
        { "gte", FilterOperator.GreaterThanOrEqual },
        { "lt", FilterOperator.LessThan },
        { "le", FilterOperator.LessThanOrEqual },
        { "lte", FilterOperator.LessThanOrEqual },
        { "contains", FilterOperator.Contains },
        { "startswith", FilterOperator.StartsWith },
        { "endswith", FilterOperator.EndsWith },
        { "in", FilterOperator.In },
        { "isnull", FilterOperator.IsNull },
        { "isnotnull", FilterOperator.IsNotNull }
    };

    public static List<FilterCriteria> Parse(string? filterString)
    {
        if (string.IsNullOrWhiteSpace(filterString))
            return new List<FilterCriteria>();

        filterString = NormalizeFilterString(filterString);

        var criteria = new List<FilterCriteria>();
        var parts = SplitByLogicalOperators(filterString);

        for (int i = 0; i < parts.Count; i++)
        {
            var part = parts[i].Trim();
            
            if (part.Equals("and", StringComparison.OrdinalIgnoreCase) || 
                part.Equals("or", StringComparison.OrdinalIgnoreCase))
                continue;

            var criteriaItem = ParseSingleFilter(part);
            if (criteriaItem != null)
            {
                if (i > 0 && i - 1 < parts.Count)
                {
                    var previousPart = parts[i - 1].Trim();
                    if (previousPart.Equals("and", StringComparison.OrdinalIgnoreCase))
                        criteriaItem.LogicalOperator = LogicalOperator.And;
                    else if (previousPart.Equals("or", StringComparison.OrdinalIgnoreCase))
                        criteriaItem.LogicalOperator = LogicalOperator.Or;
                }
                
                criteria.Add(criteriaItem);
            }
        }

        return criteria;
    }

    private static List<string> SplitByLogicalOperators(string filterString)
    {
        var parts = new List<string>();
        var current = new System.Text.StringBuilder();
        var i = 0;

        while (i < filterString.Length)
        {
            var remaining = filterString.Length - i;
            
            if (remaining >= 3 && 
                filterString.Substring(i, 3).Equals("and", StringComparison.OrdinalIgnoreCase) &&
                (i == 0 || char.IsWhiteSpace(filterString[i - 1])) &&
                (i + 3 >= filterString.Length || char.IsWhiteSpace(filterString[i + 3])))
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString().Trim());
                    current.Clear();
                }
                parts.Add("and");
                i += 3;
            }
            else if (remaining >= 2 && 
                     filterString.Substring(i, 2).Equals("or", StringComparison.OrdinalIgnoreCase) &&
                     (i == 0 || char.IsWhiteSpace(filterString[i - 1])) &&
                     (i + 2 >= filterString.Length || char.IsWhiteSpace(filterString[i + 2])))
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString().Trim());
                    current.Clear();
                }
                parts.Add("or");
                i += 2;
            }
            else
            {
                current.Append(filterString[i]);
                i++;
            }
        }

        if (current.Length > 0)
        {
            parts.Add(current.ToString().Trim());
        }

        return parts;
    }

    private static FilterCriteria? ParseSingleFilter(string filterPart)
    {
        filterPart = filterPart.Trim();
        
        var isnullMatch = Regex.Match(filterPart, @"^(\w+)\s+(isnull|isnotnull)$", RegexOptions.IgnoreCase);
        if (isnullMatch.Success)
        {
            var property = isnullMatch.Groups[1].Value.Trim();
            var operatorStr = isnullMatch.Groups[2].Value.Trim().ToLower();
            
            if (OperatorMap.TryGetValue(operatorStr, out var filterOperator))
            {
                return new FilterCriteria
                {
                    Property = property,
                    Operator = filterOperator,
                    Value = null
                };
            }
        }

        var pattern = @"^(\w+)\s+(eq|ne|neq|gt|ge|gte|lt|le|lte|contains|startswith|endswith|in)\s+(.+)$";
        var match = Regex.Match(filterPart, pattern, RegexOptions.IgnoreCase);

        if (!match.Success)
            return null;

        var property2 = match.Groups[1].Value.Trim();
        var operatorStr2 = match.Groups[2].Value.Trim().ToLower();
        var valueStr = match.Groups[3].Value.Trim();

        if (!OperatorMap.TryGetValue(operatorStr2, out var filterOperator2))
            return null;

        var value = ParseValue(valueStr, filterOperator2);

        return new FilterCriteria
        {
            Property = property2,
            Operator = filterOperator2,
            Value = value
        };
    }

    private static object? ParseValue(string valueStr, FilterOperator op)
    {
        if (op == FilterOperator.IsNull || op == FilterOperator.IsNotNull)
            return null;

        valueStr = valueStr.Trim();

        if (valueStr.StartsWith("(") && valueStr.EndsWith(")"))
        {
            var listStr = valueStr.Trim('(', ')');
            return listStr.Split(',').Select(v => v.Trim('\'', '"', ' ')).ToList();
        }

        if ((valueStr.StartsWith("'") && valueStr.EndsWith("'")) ||
            (valueStr.StartsWith("\"") && valueStr.EndsWith("\"")))
        {
            return valueStr.Substring(1, valueStr.Length - 2);
        }

        return valueStr;
    }

    private static string NormalizeFilterString(string filterString)
    {
        if (string.IsNullOrWhiteSpace(filterString))
            return filterString;

        try
        {
            var decoded = Uri.UnescapeDataString(filterString);
            decoded = decoded.Replace("+", " ");
            return decoded;
        }
        catch
        {
            return filterString.Replace("+", " ");
        }
    }
}

