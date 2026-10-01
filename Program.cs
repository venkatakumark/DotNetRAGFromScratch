using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyLlmApp.Core.Configuration;
using MyLlmApp.Core.LLM;
using MyLlmApp.Core.RAG;

HostApplicationBuilder builder =
    Host.CreateApplicationBuilder(args);

// ---------------------------------------
// Configuration
// ---------------------------------------

builder.Services.Configure<OllamaOptions>(
    builder.Configuration.GetSection(
        OllamaOptions.SectionName));

builder.Services.Configure<QdrantOptions>(
    builder.Configuration.GetSection(
        QdrantOptions.SectionName));

builder.Services.Configure<RagOptions>(
    builder.Configuration.GetSection(
        RagOptions.SectionName));

// ---------------------------------------
// RAG dependencies
// ---------------------------------------

builder.Services.AddSingleton<EmbeddingService>();

builder.Services.AddSingleton<VectorStore>();

builder.Services.AddSingleton<QueryRewriter>();

builder.Services.AddSingleton<HybridReranker>();

builder.Services.AddSingleton<ILlmService, OllamaLlmService>();

// Console application has one conversation
// for the lifetime of the application.
builder.Services.AddSingleton<ConversationHistory>();

builder.Services.AddSingleton<RagService>();

builder.Services.AddSingleton<RagEvaluator>();

// ---------------------------------------
// Build DI container
// ---------------------------------------

using IHost host =
    builder.Build();

RagService ragService =
    host.Services.GetRequiredService<RagService>();

RagEvaluator ragEvaluator =
    host.Services.GetRequiredService<RagEvaluator>();

// ---------------------------------------
// Application menu
// ---------------------------------------

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