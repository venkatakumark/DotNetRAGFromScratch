using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using MyLlmApp.Core.Configuration;

namespace MyLlmApp.Core.LLM;

public class OllamaLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public OllamaLlmService(
        IOptions<OllamaOptions> options)
    {
        _options = options.Value;

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
    }

    public async Task<string> GenerateAsync(
        string prompt)
    {
        string ollamaUrl =
            $"{_options.BaseUrl.TrimEnd('/')}/api/chat";

        var request = new
        {
            model = _options.ChatModel,

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
            $"Final Answer Model: {_options.ChatModel}");

        Console.WriteLine(
            "Calling local Ollama LLM...");

        HttpResponseMessage response =
            await _httpClient.PostAsJsonAsync(
                ollamaUrl,
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