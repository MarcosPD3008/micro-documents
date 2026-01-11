using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MicroDocuments.Domain.Ports;
using MicroDocuments.Infrastructure.BackgroundJobs;
using MicroDocuments.Infrastructure.Configuration;
using MicroDocuments.Infrastructure.ExternalServices;
using MicroDocuments.Infrastructure.Persistence;
using MicroDocuments.Infrastructure.Services;
using MicroDocuments.Domain.Entities;

namespace MicroDocuments.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddHttpContextAccessor();

        services.Configure<DocumentPublisherSettings>(
            configuration.GetSection(DocumentPublisherSettings.SectionName));

        services.Configure<ResilienceSettings>(
            configuration.GetSection(ResilienceSettings.SectionName));

        services.Configure<ApiKeySettings>(
            configuration.GetSection(ApiKeySettings.SectionName));

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<ApiKeyRepository>();
        services.AddSingleton<IApiKeyCacheService, ApiKeyCacheService>();
        services.AddScoped<IApiKeyRepository>(sp =>
        {
            var baseRepo = sp.GetRequiredService<ApiKeyRepository>();
            var cacheService = sp.GetRequiredService<IApiKeyCacheService>();
            return new CachedApiKeyRepository(baseRepo, cacheService);
        });
        
        return services;
    }

    public static IServiceCollection AddExternalServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var useMock = configuration.GetValue<bool>("DocumentPublisher:UseMock", true);
        
        if (useMock)
        {
            services.AddScoped<IDocumentPublisher, DocumentPublisherMock>();
        }
        else
        {
            services.AddScoped<IDocumentPublisher, DocumentPublisher>();
        }

        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddSingleton<ApiKeyService>();
        return services;
    }

    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<DocumentUploadBackgroundService>();
        return services;
    }

    public static async Task EnsureDatabaseCreatedAsync(
        this IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connectionString = context.Database.GetConnectionString();
        
        // Extract database path from connection string and ensure directory exists
        if (!string.IsNullOrEmpty(connectionString))
        {
            var dbPath = ExtractDatabasePath(connectionString);
            if (!string.IsNullOrEmpty(dbPath))
            {
                var directory = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }
        
        await context.Database.EnsureCreatedAsync();
        
        await SeedMasterApiKeyAsync(serviceProvider, configuration);
    }

    public static async Task SeedMasterApiKeyAsync(
        this IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        var masterKey = configuration["ApiKey:MasterKey"];
        if (string.IsNullOrWhiteSpace(masterKey))
        {
            throw new InvalidOperationException("ApiKey:MasterKey configuration is required");
        }

        using var scope = serviceProvider.CreateScope();
        var baseRepository = scope.ServiceProvider.GetRequiredService<ApiKeyRepository>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("SeedMasterApiKey");
        
        var apiKeyService = new ApiKeyService(configuration);
        var keyHash = apiKeyService.HashApiKey(masterKey);

        var existing = await baseRepository.GetByKeyHashAsync(keyHash);
        if (existing != null)
        {
            logger.LogInformation("SeedMasterApiKeyAsync - Master API key already exists");
            return;
        }

        var existingKeys = await baseRepository.GetAllActiveAsync();
        if (existingKeys.Any())
        {
            logger.LogWarning("SeedMasterApiKeyAsync - Found {Count} existing API keys that don't match the Master Key. Deleting old keys and creating new Master Key.", existingKeys.Count());
            
            foreach (var oldKey in existingKeys)
            {
                oldKey.Deleted = DateTime.UtcNow;
                oldKey.IsActive = false;
            }
            await context.SaveChangesAsync();
        }

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = "Master API Key",
            KeyHash = keyHash,
            IsActive = true,
            RateLimitPerMinute = 10000,
            Created = DateTime.UtcNow
        };

        await baseRepository.SaveAsync(apiKey);
        logger.LogInformation("SeedMasterApiKeyAsync - Master API key created successfully");
    }

    public static async Task InitializeApiKeyCacheAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<IApiKeyCacheService>();
        var baseRepository = scope.ServiceProvider.GetRequiredService<ApiKeyRepository>();
        await cacheService.InitializeAsync(baseRepository, cancellationToken);
    }

    private static string? ExtractDatabasePath(string connectionString)
    {
        var dataSourceIndex = connectionString.IndexOf("Data Source=", StringComparison.OrdinalIgnoreCase);
        if (dataSourceIndex >= 0)
        {
            var pathStart = dataSourceIndex + "Data Source=".Length;
            var path = connectionString.Substring(pathStart).Trim();
            
            if (path.StartsWith("\"") && path.EndsWith("\""))
            {
                path = path.Substring(1, path.Length - 2);
            }
            
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), path);
            }
            
            return path;
        }
        
        return null;
    }
}

