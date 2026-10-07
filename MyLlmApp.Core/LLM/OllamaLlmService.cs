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
    return await GenerateAsync(
        prompt,
        CancellationToken.None);
}

public async Task<string> GenerateAsync(
    string prompt,
    CancellationToken cancellationToken)
{
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
        $"LLM Model: {_options.ChatModel}");

    Console.WriteLine(
        "Calling local Ollama LLM...");

    HttpResponseMessage response =
        await _httpClient.PostAsJsonAsync(
            _options.BaseUrl.TrimEnd('/') + "/api/chat",
            request,
            cancellationToken);

    response.EnsureSuccessStatusCode();

    OllamaChatResponse? result =
        await response.Content
            .ReadFromJsonAsync<OllamaChatResponse>(
                cancellationToken: cancellationToken);

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