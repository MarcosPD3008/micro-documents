using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroDocuments.Domain.Entities;
using MicroDocuments.Domain.Ports;
using MicroDocuments.Infrastructure.Configuration;
using Polly;

namespace MicroDocuments.Infrastructure.ExternalServices;

public class DocumentPublisher : IDocumentPublisher
{
    private readonly DocumentPublisherSettings _settings;
    private readonly ResilienceSettings _resilienceSettings;
    private readonly ILogger<DocumentPublisher> _logger;
    private readonly IAsyncPolicy<string> _retryPolicy;

    public DocumentPublisher(
        IOptions<DocumentPublisherSettings> settings,
        IOptions<ResilienceSettings> resilienceSettings,
        ILogger<DocumentPublisher> logger)
    {
        _settings = settings.Value;
        _resilienceSettings = resilienceSettings.Value;
        _logger = logger;
        
        // Configure retry policy if enabled
        if (_resilienceSettings.RetryPolicy.Enabled)
        {
            _retryPolicy = Policy<string>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: _resilienceSettings.RetryPolicy.MaxRetryAttempts,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timespan, retryCount, context) =>
                    {
                        var errorMessage = exception.Exception?.Message ?? "Unknown error";
                        _logger.LogWarning(
                            "DocumentPublisher.PublishAsync - Retry attempt {RetryCount}/{MaxRetries} after {Delay}s. Error: {Error}",
                            retryCount,
                            _resilienceSettings.RetryPolicy.MaxRetryAttempts,
                            timespan.TotalSeconds,
                            errorMessage);
                    });
        }
        else
        {
            _retryPolicy = Policy.NoOpAsync<string>();
        }
    }

    public async Task<string> PublishAsync(Document document, byte[] fileContent, CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async (ct) =>
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            var content = new ByteArrayContent(fileContent);
            content.Headers.Add("Content-Type", document.ContentType);
            content.Headers.Add("X-Document-Id", document.Id.ToString());
            content.Headers.Add("X-Filename", document.Filename);

            _logger.LogInformation(
                "DocumentPublisher.PublishAsync - Publishing document, DocumentId: {DocumentId}, Filename: {Filename}, Url: {Url}",
                document.Id, document.Filename, _settings.Url);

            var response = await httpClient.PostAsync(_settings.Url, content, ct);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            
            _logger.LogInformation(
                "DocumentPublisher.PublishAsync - Document published successfully, DocumentId: {DocumentId}, Response: {Response}",
                document.Id, responseBody);

            return responseBody;
        }, cancellationToken);
    }

    public async Task<string> PublishStreamAsync(Document document, Stream fileStream, CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async (ct) =>
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(300); // Longer timeout for large files
            
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(document.ContentType);
            streamContent.Headers.Add("X-Document-Id", document.Id.ToString());
            streamContent.Headers.Add("X-Filename", document.Filename);
            
            _logger.LogInformation(
                "DocumentPublisher.PublishStreamAsync - Publishing document via stream, DocumentId: {DocumentId}, Filename: {Filename}, Url: {Url}",
                document.Id, document.Filename, _settings.Url);
            
            var response = await httpClient.PostAsync(_settings.Url, streamContent, ct);
            response.EnsureSuccessStatusCode();
            
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            
            _logger.LogInformation(
                "DocumentPublisher.PublishStreamAsync - Document published successfully, DocumentId: {DocumentId}, Response: {Response}",
                document.Id, responseBody);
            
            return responseBody;
        }, cancellationToken);
    }
}
