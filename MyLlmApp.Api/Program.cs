using MyLlmApp.Core.LLM;
using MyLlmApp.Core.RAG;
using MyLlmApp.Api.Models;
using MyLlmApp.Core.Configuration;
using MyLlmApp.Api.ExceptionHandling;
using MyLlmApp.Api.HealthChecks;
using MyLlmApp.Api.DocumentExtractionService;

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
builder.Services.AddSingleton<ConversationStore>();

builder.Services.AddScoped<ConversationHistory>();
builder.Services.AddScoped<RagService>();
builder.Services.AddHttpClient<OllamaHealthCheck>();
builder.Services.AddSingleton<TextChunker>();
builder.Services.AddSingleton<DocumentIngestionService>();
builder.Services
    .AddHealthChecks()
    .AddCheck<OllamaHealthCheck>("ollama")
    .AddCheck<QdrantHealthCheck>("qdrant");

// LLM provider
builder.Services.AddSingleton<ILlmService, OllamaLlmService>();
builder.Services.AddSingleton<IDocumentTextExtractor, TxtDocumentTextExtractor>();
builder.Services.AddSingleton<IDocumentTextExtractor, PdfDocumentTextExtractor>();
builder.Services.AddSingleton<IDocumentTextExtractor, DocxDocumentTextExtractor>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

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
        RagService ragService,
        ConversationStore conversationStore,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(request.ConversationId))
        {
            return Results.BadRequest(
                new
                {
                    error = "ConversationId is required."
                });
        }

        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return Results.BadRequest(
                new
                {
                    error = "Question is required."
                });
        }

        ConversationSession session =
            conversationStore.GetOrCreateSession(request.ConversationId);

        await session.Lock.WaitAsync(cancellationToken);

        try
        {
            RagResponse response =
                await ragService.AskAsync(
                    request.Question,
                    session.History,
                    cancellationToken);

            return Results.Ok(response);
        }
        finally
        {
            session.Lock.Release();
        }
    });

app.MapDelete(
    "/api/conversations/{conversationId}",
    (string conversationId, ConversationStore conversationStore) =>
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return Results.BadRequest(
                new
                {
                    error = "ConversationId is required."
                });
        }

        bool removed = conversationStore.Remove(conversationId);

        if (!removed)
        {
            return Results.NotFound(
                new
                {
                    error = "Conversation not found.",
                    conversationId
                });
        }

        return Results.Ok(
            new
            {
                message = "Conversation deleted successfully.",
                conversationId
            });
    });

app.MapPost(
    "/api/documents/upload",
    async (
        IFormFile file,
        IEnumerable<IDocumentTextExtractor> extractors,
        DocumentIngestionService ingestionService,
        CancellationToken cancellationToken) =>
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(
                new
                {
                    error = "A file is required."
                });
        }

        const long maxFileSize = 5 * 1024 * 1024;

        if (file.Length > maxFileSize)
        {
            return Results.BadRequest(
                new
                {
                    error = "File size cannot exceed 5 MB."
                });
        }

        string extension = Path.GetExtension(file.FileName);

        IDocumentTextExtractor? extractor =
            extractors.FirstOrDefault(e => e.CanHandle(extension));

        if (extractor is null)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        $"Unsupported file type '{extension}'. " +
                        "Currently supported types are .txt and .pdf."
                });
        }

        string text;

        using (Stream stream = file.OpenReadStream())
        {
            text = await extractor.ExtractTextAsync(
                stream,
                cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return Results.BadRequest(
                new
                {
                    error = "No readable text could be extracted from the document."
                });
        }

        try
        {
            DocumentIngestionResult result =
                await ingestionService.IngestDocumentAsync(
                    text: text,
                    fileName: Path.GetFileName(file.FileName),
                    contentType: file.ContentType,
                    cancellationToken: cancellationToken);

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
            when (ex.Message.StartsWith(
                "Document already indexed",
                StringComparison.Ordinal))
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    })
    .DisableAntiforgery();

app.MapGet(
    "/api/documents",
    async (
        VectorStore vectorStore,
        CancellationToken cancellationToken) =>
    {
        List<IndexedDocument> documents =
            await vectorStore.GetDocumentsAsync(cancellationToken);

        return Results.Ok(documents);
    });

app.MapDelete(
    "/api/documents/{documentId}",
    async (
        string documentId,
        VectorStore vectorStore,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            return Results.BadRequest(
                new
                {
                    error = "DocumentId is required."
                });
        }

        await vectorStore.DeleteDocumentAsync(
            documentId,
            cancellationToken);

        return Results.Ok(
            new
            {
                message = "Document deleted successfully.",
                documentId
            });
    });

app.MapHealthChecks("/health");
app.Run();

    