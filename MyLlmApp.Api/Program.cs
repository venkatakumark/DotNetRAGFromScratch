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
builder.Services.AddSingleton<DocumentFileProcessor>();

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

app.MapPost("/api/documents/upload",
    async (
        IFormFile file,
        DocumentFileProcessor fileProcessor,
        DocumentIngestionService ingestionService,
        CancellationToken cancellationToken) =>
    {
        try
        {
            DocumentFileProcessResult document =
                await fileProcessor.ProcessAsync(
                    file,
                    cancellationToken);

           DateTimeOffset now = DateTimeOffset.UtcNow;

            DocumentIngestionResult result =
            await ingestionService.IngestDocumentAsync(
            fileName: document.FileName,
            text: document.Text,
            contentType: document.ContentType,
            fileSizeBytes: document.FileSizeBytes,
            createdAt: now,
            updatedAt: now,
            cancellationToken: cancellationToken);

            return Results.Ok(result);
        }
        catch (DuplicateDocumentException ex)
        {
            return Results.Conflict(
                new
                {
                    error = "Document already indexed.",
                    documentId = ex.DocumentId,
                    fileName = ex.FileName
                });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    })
    .DisableAntiforgery();

app.MapGet("/api/documents",
    async (
        VectorStore vectorStore,
        CancellationToken cancellationToken) =>
    {
        List<IndexedDocument> documents =
            await vectorStore.GetDocumentsAsync(cancellationToken);

        return Results.Ok(documents);
    });

app.MapDelete("/api/documents/{documentId}",
    async (
        string documentId,
        VectorStore vectorStore,
        ILogger<Program> logger,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            return Results.BadRequest(new
            {
                error = "DocumentId is required."
            });
        }

        logger.LogInformation(
            "Document deletion requested. DocumentId={DocumentId}",
            documentId);

        List<IndexedDocument> documents =
            await vectorStore.GetDocumentsAsync(cancellationToken);

        IndexedDocument? existingDocument =
            documents.FirstOrDefault(d =>
                d.DocumentId.Equals(
                    documentId,
                    StringComparison.OrdinalIgnoreCase));

        if (existingDocument is null)
        {
            logger.LogWarning(
                "Document deletion rejected. Document not found. DocumentId={DocumentId}",
                documentId);

            return Results.NotFound(new
            {
                error = "Document not found.",
                documentId
            });
        }

        await vectorStore.DeleteDocumentAsync(
            documentId,
            cancellationToken);

        logger.LogInformation(
            "Document deleted successfully. DocumentId={DocumentId}, FileName={FileName}",
            documentId,
            existingDocument.FileName);

        return Results.Ok(new
        {
            message = "Document deleted successfully.",
            documentId
        });
    });
app.MapPut("/api/documents/{documentId}",
    async (
        string documentId,
        IFormFile file,
        DocumentFileProcessor fileProcessor,
        VectorStore vectorStore,
        DocumentIngestionService ingestionService,
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

        // Verify that the document being replaced exists.
        List<IndexedDocument> documents =
            await vectorStore.GetDocumentsAsync(
                cancellationToken);

        IndexedDocument? existingDocument =
            documents.FirstOrDefault(
                d => d.DocumentId.Equals(
                    documentId,
                    StringComparison.OrdinalIgnoreCase));

        if (existingDocument is null)
        {
            return Results.NotFound(
                new
                {
                    error = "Document not found.",
                    documentId
                });
        }

        try
        {
            DocumentFileProcessResult document =
                await fileProcessor.ProcessAsync(
                    file,
                    cancellationToken);

            DocumentUpdateResult result =
                await ingestionService.ReplaceDocumentAsync(
                    oldDocumentId: documentId,
                    text: document.Text,
                    fileName: document.FileName,
                    contentType: document.ContentType,
                    fileSizeBytes: document.FileSizeBytes,
                    originalCreatedAt: existingDocument.CreatedAt,
                    cancellationToken: cancellationToken);

            return Results.Ok(result);
        }
        catch (DuplicateDocumentException ex)
        {
            return Results.Conflict(
                new
                {
                    error = "Document already indexed.",
                    documentId = ex.DocumentId,
                    fileName = ex.FileName
                });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    })
    .DisableAntiforgery();
app.MapHealthChecks("/health");
app.Run();

    