using Microsoft.Extensions.Options;
using MyLlmApp.Core.Configuration;
using MyLlmApp.Core.LLM;

namespace MyLlmApp.Core.RAG;

public class RagService
{
    private readonly EmbeddingService _embeddingService;
    private readonly VectorStore _vectorStore;
    private readonly QueryRewriter _queryRewriter;
    private readonly ConversationHistory _conversationHistory;
    private readonly HybridReranker _reranker;
    private readonly ILlmService _llmService;
    private readonly RagOptions _options;

    public RagService(
        EmbeddingService embeddingService,
        VectorStore vectorStore,
        QueryRewriter queryRewriter,
        ConversationHistory conversationHistory,
        HybridReranker reranker,
        ILlmService llmService,
        IOptions<RagOptions> options)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _queryRewriter = queryRewriter;
        _conversationHistory = conversationHistory;
        _reranker = reranker;
        _llmService = llmService;
        _options = options.Value;
    }

    // =====================================
    // Existing method
    //
    // Used by Console application.
    // Uses the ConversationHistory supplied
    // through dependency injection.
    // =====================================

    public async Task<RagResponse> AskAsync(
        string question)
    {
        return await AskInternalAsync(
            question,
            _conversationHistory,CancellationToken.None);
    }

    // =====================================
    // New overload
    //
    // Used by Web API.
    // Allows the API to supply the history
    // associated with a conversationId.
    // =====================================

    public async Task<RagResponse> AskAsync(
        string question,
        ConversationHistory conversationHistory,
        CancellationToken cancellationToken)
    {
        return await AskInternalAsync(
            question,
            conversationHistory,CancellationToken.None);
    }

    // =====================================
    // Internal RAG pipeline
    // =====================================

    private async Task<RagResponse> AskInternalAsync(
        string question,
        ConversationHistory conversationHistory,
        CancellationToken cancellationToken)
    {
        // ---------------------------------
        // 1. Rewrite question using history
        // ---------------------------------

        string rewrittenQuestion =
            await _queryRewriter.RewriteAsync(
                question,
                conversationHistory,cancellationToken);

        Console.WriteLine();
        Console.WriteLine(
            $"Original Question: {question}");

        Console.WriteLine(
            $"Rewritten Question: {rewrittenQuestion}");

        // ---------------------------------
        // 2. Create query embedding
        // ---------------------------------

        Console.WriteLine();
        Console.WriteLine(
            "Creating query embedding...");

        float[] queryEmbedding =
            await _embeddingService
                .CreateEmbeddingAsync(
                    rewrittenQuestion,cancellationToken);

        // ---------------------------------
        // 3. Search Qdrant
        //
        // Retrieve more candidates than
        // we finally send to the LLM.
        // ---------------------------------

        Console.WriteLine(
            "Searching Qdrant...");

        List<SearchResult> results =
            await _vectorStore.SearchAsync(
                queryEmbedding,
                limit: _options.TopK,
                 cancellationToken: cancellationToken);

        Console.WriteLine();

        Console.WriteLine(
            "Retrieved context:");

        foreach (SearchResult result in results)
        {
            Console.WriteLine(
                $"Score: {result.Score:F4} | " +
                $"Source: {result.Source} | " +
                $"Chunk: {result.ChunkIndex}");
        }

        // ---------------------------------
        // 4. Dynamic similarity filtering
        // ---------------------------------

        List<SearchResult> relevantResults = [];

        if (results.Count > 0)
        {
            float bestScore =
                results.Max(
                    result => result.Score);

            float relativeThreshold =
                bestScore -
                _options.MaximumScoreGap;

            float effectiveThreshold =
                Math.Max(
                    _options.MinimumScore,
                    relativeThreshold);

            relevantResults =
                results
                    .Where(
                        result =>
                            result.Score >=
                            effectiveThreshold)
                    .ToList();

            Console.WriteLine();

            Console.WriteLine(
                $"Best score: " +
                $"{bestScore:F4}");

            Console.WriteLine(
                $"Minimum threshold: " +
                $"{_options.MinimumScore:F4}");

            Console.WriteLine(
                $"Relative threshold: " +
                $"{relativeThreshold:F4}");

            Console.WriteLine(
                $"Effective threshold: " +
                $"{effectiveThreshold:F4}");
        }

        Console.WriteLine();

        Console.WriteLine(
            $"Relevant results before reranking: " +
            $"{relevantResults.Count}");

        foreach (SearchResult result
                 in relevantResults)
        {
            Console.WriteLine(
                $"Score: {result.Score:F4} | " +
                $"Source: {result.Source} | " +
                $"Chunk: {result.ChunkIndex}");
        }

        // ---------------------------------
        // 5. No relevant context
        // ---------------------------------

        if (relevantResults.Count == 0)
        {
            string noAnswer =
                "I could not find relevant information " +
                "in the provided documents.";

            SaveConversation(
                conversationHistory,
                question,
                noAnswer);

            return new RagResponse
            {
                Answer = noAnswer
            };
        }

        // ---------------------------------
        // 6. Hybrid reranking
        //
        // Vector score  = semantic similarity
        // Keyword score = lexical similarity
        //
        // HybridReranker combines both.
        // ---------------------------------

        List<RerankedResult> rerankedResults =
            _reranker.Rerank(
                rewrittenQuestion,
                relevantResults);

        Console.WriteLine();

        Console.WriteLine(
            "Reranked results:");

        foreach (RerankedResult result
                 in rerankedResults)
        {
            Console.WriteLine(
                $"Vector: " +
                $"{result.VectorScore:F4} | " +

                $"Keyword: " +
                $"{result.KeywordScore:F4} | " +

                $"Final: " +
                $"{result.FinalScore:F4} | " +

                $"Source: " +
                $"{result.SearchResult.Source} | " +

                $"Chunk: " +
                $"{result.SearchResult.ChunkIndex}");
        }

        // ---------------------------------
        // 7. Keep best results after reranking
        // ---------------------------------

        relevantResults =
            rerankedResults
                .Take(
                    _options.FinalContextCount)
                .Select(
                    result =>
                        result.SearchResult)
                .ToList();

        Console.WriteLine();

        Console.WriteLine(
            "Final context selected:");

        foreach (SearchResult result
                 in relevantResults)
        {
            Console.WriteLine(
                $"Score: {result.Score:F4} | " +
                $"Source: {result.Source} | " +
                $"Chunk: {result.ChunkIndex}");
        }

        // ---------------------------------
        // 8. Build context
        // ---------------------------------

        string context =
            BuildContext(
                relevantResults);

        // ---------------------------------
        // 9. Build final RAG prompt
        // ---------------------------------

        string prompt = $"""
            Answer the user's question using only
            the information provided in the context.

            If the answer cannot be found in the context,
            say that the information is not available
            in the provided documents.

            Do not use outside knowledge.

            Context:
            {context}

            User Question:
            {rewrittenQuestion}
            """;

        // ---------------------------------
        // 10. Send context to final LLM
        // ---------------------------------

        Console.WriteLine();

        Console.WriteLine(
            "Sending context to LLM...");

        string answer =
            await _llmService.GenerateAsync(
                prompt);

        // ---------------------------------
        // 11. Build source list
        // ---------------------------------

        List<RagSource> sources =
            relevantResults
                .Select(
                    result =>
                        new RagSource
                        {
                            Source =
                                result.Source,

                            ChunkIndex =
                                result.ChunkIndex,

                            Score =
                                result.Score
                        })
                .ToList();

        // ---------------------------------
        // 12. Save conversation
        // ---------------------------------

        SaveConversation(
            conversationHistory,
            question,
            answer);

        // ---------------------------------
        // 13. Return structured response
        // ---------------------------------

        return new RagResponse
        {
            Answer = answer,
            Sources = sources
        };
    }

    // =====================================
    // Save conversation
    // =====================================

    private static void SaveConversation(
        ConversationHistory conversationHistory,
        string question,
        string answer)
    {
        conversationHistory
            .AddUserMessage(
                question);

        conversationHistory
            .AddAssistantMessage(
                answer);
    }

    // =====================================
    // Build context for final LLM
    // =====================================

    private static string BuildContext(
        List<SearchResult> results)
    {
        List<string> contextParts = [];

        foreach (SearchResult result
                 in results)
        {
            contextParts.Add(
                $"""
                Source: {result.Source}
                Chunk: {result.ChunkIndex}

                {result.Text}
                """);
        }

        return string.Join(
            "\n\n--------------------\n\n",
            contextParts);
    }
}