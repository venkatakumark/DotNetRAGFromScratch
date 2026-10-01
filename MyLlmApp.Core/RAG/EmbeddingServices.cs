using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using MyLlmApp.Core.Configuration;

namespace MyLlmApp.Core.RAG;

public class EmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public EmbeddingService(
        IOptions<OllamaOptions> options)
    {
        _options = options.Value;

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public async Task<float[]> CreateEmbeddingAsync(
        string text)
    {
        string url =
            $"{_options.BaseUrl.TrimEnd('/')}/api/embed";

        var request = new
        {
            model = _options.EmbeddingModel,
            input = text
        };

        HttpResponseMessage response =
            await _httpClient.PostAsJsonAsync(
                url,
                request);

        response.EnsureSuccessStatusCode();

        OllamaEmbeddingResponse? result =
            await response.Content
                .ReadFromJsonAsync<OllamaEmbeddingResponse>();

        if (result?.Embeddings == null ||
            result.Embeddings.Count == 0)
        {
            throw new InvalidOperationException(
                "Ollama returned no embeddings.");
        }

        return result.Embeddings[0];
    }

    private class OllamaEmbeddingResponse
    {
        public List<float[]> Embeddings { get; set; } = [];
    }
}