using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using MyLlmApp.Core.Configuration;

namespace MyLlmApp.Core.RAG;

public class QueryRewriter
{

   private readonly HttpClient _httpClient;
private readonly OllamaOptions _options;

    public QueryRewriter(
        IOptions<OllamaOptions> options)
    {
        _options = options.Value;

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(3)
        };
    }

   public async Task<string> RewriteAsync(
    string currentQuestion,
    ConversationHistory history)
{
    // ---------------------------------------
    // 1. No history = no rewrite required
    // ---------------------------------------

    if (!history.HasHistory)
    {
        Console.WriteLine();
        Console.WriteLine(
            "No conversation history. Skipping query rewrite.");

        return currentQuestion;
    }

    // ---------------------------------------
    // 2. Build conversation history
    // ---------------------------------------

    string conversation =
        BuildConversation(history);

    // ---------------------------------------
    // 3. Build a simple prompt
    //    Small models work better with
    //    direct and explicit instructions.
    // ---------------------------------------

    string prompt = $"""
        Rewrite the current question so it can be understood
        without the previous conversation.

        Replace words like "it", "them", "they", "that",
        "those", "he", "she" with what they refer to.

        Do not answer the question.
        Return only one rewritten question.

        Previous conversation:
        {conversation}

        Current question:
        {currentQuestion}

        Rewritten question:
        """;

    // ---------------------------------------
    // 4. Build Ollama request
    // ---------------------------------------

    var request = new
    {
        model = _options.QueryRewriteModel,

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

    // ---------------------------------------
    // 5. Call local Ollama
    // ---------------------------------------

    Console.WriteLine();
    Console.WriteLine(
        $"Query Rewriter Model: {_options.QueryRewriteModel}");

    Console.WriteLine(
        "Calling local Ollama query rewriter...");

    HttpResponseMessage response =
        await _httpClient.PostAsJsonAsync(
            _options.BaseUrl.TrimEnd('/') + "/api/chat",
            request);

    response.EnsureSuccessStatusCode();

    // ---------------------------------------
    // 6. Read Ollama response
    // ---------------------------------------

    OllamaChatResponse? result =
        await response.Content
            .ReadFromJsonAsync<OllamaChatResponse>();

    string? rewrittenQuestion =
        result?.Message?.Content;

    // ---------------------------------------
    // 7. Fallback if Ollama returned
    //    an empty response
    // ---------------------------------------

    if (string.IsNullOrWhiteSpace(
        rewrittenQuestion))
    {
        Console.WriteLine(
            "Query rewriter returned an empty response.");

        Console.WriteLine(
            "Using original question.");

        return currentQuestion;
    }

    // ---------------------------------------
    // 8. Clean result
    // ---------------------------------------

    rewrittenQuestion =
        rewrittenQuestion.Trim();

    // ---------------------------------------
    // 9. Debug output
    // ---------------------------------------

    Console.WriteLine(
        $"Query Rewrite Result: {rewrittenQuestion}");

    // ---------------------------------------
    // 10. Return standalone question
    // ---------------------------------------

    return rewrittenQuestion;
}

    private static string BuildConversation(
    ConversationHistory history)
    {
    const int maxMessages = 6;
    // 6 messages = approximately 3 User/Assistant turns

    StringBuilder builder = new();

    var recentMessages =
        history.Messages
            .TakeLast(maxMessages);

        foreach (ConversationMessage message
             in recentMessages)
        {
            builder.AppendLine(
             $"{message.Role}: {message.Content}");
        }

        return builder.ToString();
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