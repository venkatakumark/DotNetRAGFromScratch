using MyLlmApp.RAG;
using MyLlmApp.LLM;

ILlmService llmService =
    new OllamaLlmService();
var embeddingService =
    new EmbeddingService();

var vectorStore =
    new VectorStore();

var queryRewriter =
    new QueryRewriter();

var conversationHistory =
    new ConversationHistory();
    var hybridReranker =
    new HybridReranker();

var ragService =
    new RagService(
        embeddingService,
        vectorStore,
        queryRewriter,
        conversationHistory,
        hybridReranker,
        llmService);

var ragEvaluator =
    new RagEvaluator(
        ragService,
        conversationHistory);

Console.WriteLine(
    "======================================");

Console.WriteLine(
    "           RAG APPLICATION");

Console.WriteLine(
    "======================================");

Console.WriteLine();
Console.WriteLine("1 - Interactive RAG");
Console.WriteLine("2 - Run RAG Evaluation");
Console.WriteLine();

Console.Write("Select option: ");

string? option =
    Console.ReadLine();

if (option == "2")
{
    await ragEvaluator.RunAsync();
    return;
}

// ---------------------------------------
// Interactive RAG
// ---------------------------------------

Console.WriteLine();
Console.WriteLine(
    "======================================");

Console.WriteLine(
    "    CONVERSATIONAL RAG ASSISTANT");

Console.WriteLine(
    "======================================");

Console.WriteLine();
Console.WriteLine(
    "Ask questions about the company policy.");

Console.WriteLine(
    "Type 'exit' to quit.");
Console.WriteLine();


while (true)
{
    Console.WriteLine();
    Console.Write("Question: ");

    string? question =
        Console.ReadLine();

    if (string.IsNullOrWhiteSpace(question))
    {
        continue;
    }

    if (question.Equals(
            "exit",
            StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    Console.WriteLine();
    Console.WriteLine(
        "Searching and generating answer...");

    try
    {
        RagResponse response =
            await ragService.AskAsync(
                question);

        Console.WriteLine();
        Console.WriteLine("Answer:");

        Console.WriteLine(
            response.Answer);

        if (response.Sources.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Sources:");

            foreach (RagSource source
                     in response.Sources)
            {
                Console.WriteLine(
                    $"- {source.Source} " +
                    $"(Chunk {source.ChunkIndex}, " +
                    $"Score {source.Score:F4})");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine(
            $"Error: {ex.Message}");
    }
}