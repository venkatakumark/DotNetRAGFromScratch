using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MyLlmApp.Core.Configuration;

namespace MyLlmApp.Api.HealthChecks;

public class OllamaHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public OllamaHealthCheck(
        HttpClient httpClient,
        IOptions<OllamaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string url =
                _options.BaseUrl.TrimEnd('/') +
                "/api/tags";

            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    url,
                    cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy(
                    "Ollama is reachable.");
            }

            return HealthCheckResult.Unhealthy(
                $"Ollama returned HTTP {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Ollama is not reachable.",
                exception);
        }
    }
}