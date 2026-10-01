using System.Net.Http.Json;

namespace MyLlmApp.LLM;

public class OllamaLlmService : ILlmService
{
    private readonly HttpClient _httpClient;

    private const string OllamaUrl =
        "http://localhost:11434/api/chat";

    private const string Model =
        "granite4.2:latest";

    public OllamaLlmService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
    }

    public async Task<string> GenerateAsync(
        string prompt)
    {
        var request = new
        {
            model = Model,

            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },

            stream = false,

            options = new
            {
                temperature = 0
            }
        };

        Console.WriteLine();
        Console.WriteLine(
            $"Final Answer Model: {Model}");

        Console.WriteLine(
            "Calling local Ollama LLM...");

        HttpResponseMessage response =
            await _httpClient.PostAsJsonAsync(
                OllamaUrl,
                request);

        response.EnsureSuccessStatusCode();

        OllamaChatResponse? result =
            await response.Content
                .ReadFromJsonAsync<OllamaChatResponse>();

        string? answer =
            result?.Message?.Content;

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new InvalidOperationException(
                "Ollama returned an empty response.");
        }

        return answer.Trim();
    }

    private class OllamaChatResponse
    {
        public OllamaMessage? Message { get; set; }
    }

    private class OllamaMessage
    {
        public string Content { get; set; } = "";
    }
}