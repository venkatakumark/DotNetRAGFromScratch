using System.Net.Http.Json;
using System.Text.Json;

namespace MyLlmApp.RAG;

public class EmbeddingService
{
    private readonly HttpClient _httpClient;

    private const string OllamaUrl =
        "http://localhost:11434/api/embed";

    private const string Model =
        "nomic-embed-text";

    public EmbeddingService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<float[]> GenerateEmbeddingAsync(
        string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "Text cannot be empty.",
                nameof(text));
        }

        var request = new
        {
            model = Model,
            input = text
        };

        var response = await _httpClient.PostAsJsonAsync(
            OllamaUrl,
            request);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();

        if (result?.Embeddings == null ||
            result.Embeddings.Count == 0)
        {
            throw new InvalidOperationException(
                "Ollama returned no embedding.");
        }

        return result.Embeddings[0];
    }

    private class OllamaEmbeddingResponse
    {
        public List<float[]> Embeddings { get; set; } = [];
    }
}