using MyLlmApp.Core.LLM;
using MyLlmApp.Core.RAG;
using MyLlmApp.Api.Models;
using MyLlmApp.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OllamaOptions>(
    builder.Configuration.GetSection(
        OllamaOptions.SectionName));

builder.Services.Configure<QdrantOptions>(
    builder.Configuration.GetSection(
        QdrantOptions.SectionName));

builder.Services.Configure<RagOptions>(
    builder.Configuration.GetSection(
        RagOptions.SectionName));

// OpenAPI
builder.Services.AddOpenApi();

// RAG dependencies
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<VectorStore>();
builder.Services.AddSingleton<QueryRewriter>();
builder.Services.AddSingleton<HybridReranker>();

builder.Services.AddScoped<ConversationHistory>();
builder.Services.AddScoped<RagService>();

// LLM provider
builder.Services.AddSingleton<ILlmService, OllamaLlmService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/", () => "MyLlmApp RAG API is running");
app.MapPost(
    "/api/rag/ask",
    async (
        RagRequest request,
        RagService ragService) =>
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return Results.BadRequest(
                new
                {
                    error = "Question is required."
                });
        }

        RagResponse response =
            await ragService.AskAsync(
                request.Question);

        return Results.Ok(response);
    });
app.Run();