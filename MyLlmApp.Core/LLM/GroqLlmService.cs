
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MyLlmApp.Core.LLM;

public sealed class GroqLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public GroqLlmService(
        HttpClient httpClient,
        string apiKey,
        string model)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException(
                "Groq API key is missing.",
                nameof(apiKey));
        }

        _httpClient = httpClient;
        _httpClient.BaseAddress =
            new Uri("https://api.groq.com/openai/v1/");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer", apiKey);

        _httpClient.Timeout = TimeSpan.FromMinutes(3);
        _model = model;
    }

    public Task<string> GenerateAsync(string prompt)
    {
        return GenerateAsync(
            prompt,
            CancellationToken.None);
    }

    public async Task<string> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            model = _model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature = 0,
            stream = false
        };

        using HttpResponseMessage response =
            await _httpClient.PostAsJsonAsync(
                "chat/completions",
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        GroqChatResponse? result =
            await response.Content
                .ReadFromJsonAsync<GroqChatResponse>(
                    cancellationToken:
                        cancellationToken);

        string? answer =
            result?.Choices?.FirstOrDefault()
                ?.Message?.Content;

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new InvalidOperationException(
                "Groq returned an empty response.");
        }

        return answer.Trim();
    }

    private sealed class GroqChatResponse
    {
        [JsonPropertyName("choices")]
        public List<GroqChoice>? Choices { get; set; }
    }

    private sealed class GroqChoice
    {
        [JsonPropertyName("message")]
        public GroqMessage? Message { get; set; }
    }

    private sealed class GroqMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
