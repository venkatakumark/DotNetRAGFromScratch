using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MyLlmApp.Core.Configuration;
using Qdrant.Client;

namespace MyLlmApp.Api.HealthChecks;

public class QdrantHealthCheck : IHealthCheck
{
    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;

    public QdrantHealthCheck(
        IOptions<QdrantOptions> options)
    {
        _options = options.Value;

        _client = new QdrantClient(
            host: _options.Host,
            port: _options.Port);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            bool exists =
                await _client.CollectionExistsAsync(
                    _options.CollectionName,
                    cancellationToken);

            if (!exists)
            {
                return HealthCheckResult.Unhealthy(
                    $"Qdrant is reachable, but collection " +
                    $"'{_options.CollectionName}' does not exist.");
            }

            return HealthCheckResult.Healthy(
                $"Qdrant is reachable and collection " +
                $"'{_options.CollectionName}' exists.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Qdrant is not reachable.",
                exception);
        }
    }
}