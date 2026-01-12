using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Reflection;

namespace MicroDocuments.Api.Swagger;

public class DefaultValueSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == null)
            return;

        foreach (var property in context.Type.GetProperties())
        {
            var defaultValueAttribute = property.GetCustomAttribute<DefaultValueAttribute>();
            if (defaultValueAttribute != null && schema.Properties != null && schema.Properties.ContainsKey(property.Name))
            {
                var propertySchema = schema.Properties[property.Name];
                if (propertySchema != null)
                {
                    propertySchema.Default = CreateOpenApiAny(defaultValueAttribute.Value);
                    propertySchema.Description = propertySchema.Description != null
                        ? $"{propertySchema.Description} (Default: {defaultValueAttribute.Value})"
                        : $"Default: {defaultValueAttribute.Value}";
                }
            }
        }
    }

    private static IOpenApiAny CreateOpenApiAny(object? value)
    {
        if (value == null)
            return new OpenApiNull();
        
        if (value is string str)
            return new OpenApiString(str);
        
        if (value is int i)
            return new OpenApiInteger(i);
        
        if (value is bool b)
            return new OpenApiBoolean(b);
        
        if (value is double d)
            return new OpenApiDouble(d);
        
        return new OpenApiString(value.ToString() ?? string.Empty);
    }
}

