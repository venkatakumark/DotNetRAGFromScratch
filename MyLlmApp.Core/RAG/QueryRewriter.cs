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
    ConversationHistory history,
    CancellationToken cancellationToken)
{
    if (!history.HasHistory)
    {
        Console.WriteLine();
        Console.WriteLine(
            "No conversation history. Skipping query rewrite.");

        return currentQuestion;
    }

    string conversation =
        BuildConversation(history);

    string prompt = $"""
        Convert the current question into a standalone
        search question using the previous conversation.

        Rules:
        - Preserve the meaning of the current question.
        - Resolve pronouns and references using the previous conversation.
        - Replace words such as "it", "them", "they", "that",
          "those", "he", and "she" with the specific subject
          they refer to.
        - Include the important subject from the previous conversation
          when the current question depends on it.
        - Do not replace one pronoun with another pronoun.
        - Do not invent information that is not present in the conversation.
        - Do not answer the question.
        - Return only the rewritten question.
        - If the current question is already standalone,
          return it unchanged.

        Example:

        Previous conversation:
        User: Who approves leave requests?
        Assistant: Managers approve leave requests.

        Current question:
        How many days in advance should I request it?

        Rewritten question:
        How many days in advance should I submit a leave request?

        Previous conversation:
        {conversation}

        Current question:
        {currentQuestion}

        Rewritten question:
        """;

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

    Console.WriteLine();
    Console.WriteLine(
        $"Query Rewriter Model: {_options.QueryRewriteModel}");

    Console.WriteLine(
        "Calling local Ollama query rewriter...");

    HttpResponseMessage response =
        await _httpClient.PostAsJsonAsync(
            _options.BaseUrl.TrimEnd('/') + "/api/chat",
            request,
            cancellationToken);

    response.EnsureSuccessStatusCode();

    OllamaChatResponse? result =
        await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
            cancellationToken: cancellationToken);

    string? rewrittenQuestion =
        result?.Message?.Content;

    if (string.IsNullOrWhiteSpace(
        rewrittenQuestion))
    {
        Console.WriteLine(
            "Query rewriter returned an empty response.");

        Console.WriteLine(
            "Using original question.");

        return currentQuestion;
    }

    rewrittenQuestion =
        rewrittenQuestion.Trim();

    Console.WriteLine(
        $"Query Rewrite Result: {rewrittenQuestion}");

    return rewrittenQuestion;
}

    private static string BuildConversation(
        ConversationHistory history)
    {
        const int maxMessages = 6;

        // 6 messages =
        // approximately 3 User/Assistant turns

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