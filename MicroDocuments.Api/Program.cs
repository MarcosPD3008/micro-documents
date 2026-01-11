using System.Text.Json.Serialization;
using MicroDocuments.Application.Extensions;
using MicroDocuments.Infrastructure.Extensions;
using MicroDocuments.Infrastructure.Middleware;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
    if (!Directory.Exists(logsDirectory))
    {
        Directory.CreateDirectory(logsDirectory);
    }

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: Path.Combine(logsDirectory, "micro-documents-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    builder.Host.UseSerilog();

    Log.Information("Starting MicroDocuments API");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BHD Document Asset Gateway",
        Version = "v1",
        Description = "Internal operations for managing document uploads and searching uploaded documents"
    });
    
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API Key Authentication using X-API-Key header. Example: dev-api-key-default",
        Name = "X-API-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
    
    c.EnableAnnotations();
    
    c.UseInlineDefinitionsForEnums();
});

builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddExternalServices(builder.Configuration);
builder.Services.AddBackgroundServices();

builder.Services.AddUseCases();

var app = builder.Build();

await app.Services.EnsureDatabaseCreatedAsync(builder.Configuration);
await app.Services.InitializeApiKeyCacheAsync();

app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
