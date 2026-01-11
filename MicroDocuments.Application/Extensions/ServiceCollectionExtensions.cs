using Microsoft.Extensions.DependencyInjection;
using MicroDocuments.Application.UseCases;

namespace MicroDocuments.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<UploadDocumentUseCase>();
        services.AddScoped<SearchDocumentsUseCase>();
        services.AddScoped<SearchDocumentsPagedUseCase>();
        return services;
    }
}

