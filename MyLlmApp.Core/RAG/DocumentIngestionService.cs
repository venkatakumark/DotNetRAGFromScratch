
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MyLlmApp.Core.RAG;

public class DocumentIngestionService
{
    private readonly TextChunker _chunker;
    private readonly EmbeddingService _embeddingService;
    private readonly VectorStore _vectorStore;
    private readonly ILogger<DocumentIngestionService> _logger;

    public DocumentIngestionService(
        TextChunker chunker,
        EmbeddingService embeddingService,
        VectorStore vectorStore,
        ILogger<DocumentIngestionService> logger)
    {
        _chunker = chunker;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    // Original batch ingestion method retained for compatibility.
    public async Task IngestDocumentsAsync(
        List<(string Text, string Source)> documents)
    {
        await _vectorStore.CreateCollectionAsync();

        ulong nextId = 1000;

        foreach (var document in documents)
        {
            List<string> chunks =
                _chunker.SplitText(document.Text);

            for (int i = 0; i < chunks.Count; i++)
            {
                string chunk = chunks[i];

                float[] embedding =
                    await _embeddingService.CreateEmbeddingAsync(
                        chunk,
                        CancellationToken.None);

                await _vectorStore.AddChunkAsync(
                    nextId++,
                    embedding,
                    chunk,
                    document.Source,
                    i);
            }
        }
    }

    // Existing ingestion method retained for compatibility.
    public Task<DocumentIngestionResult> IngestDocumentAsync(
        string text,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        return IngestDocumentAsync(
            text: text,
            fileName: fileName,
            contentType: contentType,
            fileSizeBytes: null,
            createdAt: now,
            updatedAt: now,
            cancellationToken: cancellationToken);
    }

    // New metadata-aware ingestion overload.
    public async Task<DocumentIngestionResult> IngestDocumentAsync(
        string text,
        string fileName,
        string contentType,
        long? fileSizeBytes,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException(
                "Document text cannot be empty.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException(
                "File name cannot be empty.");

        if (fileSizeBytes is < 0)
            throw new ArgumentOutOfRangeException(
                nameof(fileSizeBytes),
                "File size cannot be negative.");

        cancellationToken.ThrowIfCancellationRequested();

        Stopwatch stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Document ingestion started. " +
            "FileName={FileName}, ContentType={ContentType}",
            fileName,
            contentType);

        await _vectorStore.CreateCollectionAsync();

        byte[] contentBytes = Encoding.UTF8.GetBytes(text);
        byte[] hashBytes = SHA256.HashData(contentBytes);

        string documentHash =
            Convert.ToHexString(hashBytes).ToLowerInvariant();

        _logger.LogDebug(
            "Document hash calculated. " +
            "FileName={FileName}, DocumentHash={DocumentHash}",
            fileName,
            documentHash);

        IndexedDocument? existingDocument =
            await _vectorStore.FindDocumentByHashAsync(
                documentHash,
                cancellationToken);

        if (existingDocument is not null)
        {
            _logger.LogWarning(
                "Duplicate document rejected. " +
                "FileName={FileName}, ExistingDocumentId={DocumentId}",
                fileName,
                existingDocument.DocumentId);

            throw new DuplicateDocumentException(
                existingDocument.DocumentId,
                existingDocument.FileName);
        }

        string documentId = Guid.NewGuid().ToString("N");

        List<string> chunks = _chunker.SplitText(text);

        if (chunks.Count == 0)
            throw new InvalidOperationException(
                "Document chunking produced no chunks.");

        _logger.LogInformation(
            "Document chunking completed. " +
            "FileName={FileName}, DocumentId={DocumentId}, " +
            "ChunkCount={ChunkCount}",
            fileName,
            documentId,
            chunks.Count);

        for (int i = 0; i < chunks.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string chunk = chunks[i];

            float[] embedding =
                await _embeddingService.CreateEmbeddingAsync(
                    chunk,
                    cancellationToken);

            ulong pointId = CreatePointId(documentId, i);

            if (fileSizeBytes.HasValue)
            {
                // New Qdrant overload with document metadata.
                await _vectorStore.AddChunkAsync(
                    id: pointId,
                    embedding: embedding,
                    text: chunk,
                    source: fileName,
                    chunkIndex: i,
                    documentId: documentId,
                    contentType: contentType,
                    documentHash: documentHash,
                    fileSizeBytes: fileSizeBytes.Value,
                    createdAt: createdAt,
                    updatedAt: updatedAt,
                    cancellationToken: cancellationToken);
            }
            else
            {
                // Existing Qdrant overload retained for compatibility.
                await _vectorStore.AddChunkAsync(
                    id: pointId,
                    embedding: embedding,
                    text: chunk,
                    source: fileName,
                    chunkIndex: i,
                    documentId: documentId,
                    contentType: contentType,
                    documentHash: documentHash,
                    cancellationToken: cancellationToken);
            }
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "Document ingestion completed. " +
            "FileName={FileName}, DocumentId={DocumentId}, " +
            "ChunkCount={ChunkCount}, DurationMs={DurationMs}",
            fileName,
            documentId,
            chunks.Count,
            stopwatch.ElapsedMilliseconds);

        return new DocumentIngestionResult
        {
            DocumentId = documentId,
            FileName = fileName,
            ChunkCount = chunks.Count
        };
    }

    // Existing replacement method retained for compatibility.
    public async Task<DocumentUpdateResult> ReplaceDocumentAsync(
        string oldDocumentId,
        string text,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(oldDocumentId))
            throw new ArgumentException(
                "Old document ID is required.");

        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException(
                "Replacement document text cannot be empty.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException(
                "Replacement file name is required.");

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Document replacement started. " +
            "OldDocumentId={OldDocumentId}, FileName={FileName}",
            oldDocumentId,
            fileName);

        DocumentIngestionResult newDocument =
            await IngestDocumentAsync(
                text: text,
                fileName: fileName,
                contentType: contentType,
                cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Replacement document indexed. " +
            "OldDocumentId={OldDocumentId}, " +
            "NewDocumentId={NewDocumentId}, " +
            "FileName={FileName}, ChunkCount={ChunkCount}",
            oldDocumentId,
            newDocument.DocumentId,
            newDocument.FileName,
            newDocument.ChunkCount);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _vectorStore.DeleteDocumentAsync(
                oldDocumentId,
                cancellationToken);

            _logger.LogInformation(
                "Old document deleted after replacement. " +
                "OldDocumentId={OldDocumentId}, " +
                "NewDocumentId={NewDocumentId}",
                oldDocumentId,
                newDocument.DocumentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Document replacement failed after new version " +
                "was indexed. Attempting rollback. " +
                "OldDocumentId={OldDocumentId}, " +
                "NewDocumentId={NewDocumentId}",
                oldDocumentId,
                newDocument.DocumentId);

            try
            {
                await _vectorStore.DeleteDocumentAsync(
                    newDocument.DocumentId,
                    CancellationToken.None);

                _logger.LogWarning(
                    "Replacement rollback completed. " +
                    "New document removed. " +
                    "OldDocumentId={OldDocumentId}, " +
                    "NewDocumentId={NewDocumentId}",
                    oldDocumentId,
                    newDocument.DocumentId);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogCritical(
                    rollbackEx,
                    "Replacement rollback failed. " +
                    "Both document versions may exist. " +
                    "OldDocumentId={OldDocumentId}, " +
                    "NewDocumentId={NewDocumentId}",
                    oldDocumentId,
                    newDocument.DocumentId);
            }

            throw;
        }

        _logger.LogInformation(
            "Document replacement completed. " +
            "OldDocumentId={OldDocumentId}, " +
            "NewDocumentId={NewDocumentId}, FileName={FileName}",
            oldDocumentId,
            newDocument.DocumentId,
            newDocument.FileName);

        return new DocumentUpdateResult
        {
            OldDocumentId = oldDocumentId,
            NewDocumentId = newDocument.DocumentId,
            FileName = newDocument.FileName,
            ChunkCount = newDocument.ChunkCount
        };
    }

public async Task<DocumentUpdateResult> ReplaceDocumentAsync(
    string oldDocumentId,
    string text,
    string fileName,
    string contentType,
    long fileSizeBytes,
    DateTimeOffset? originalCreatedAt,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(oldDocumentId))
        throw new ArgumentException(
            "Old document ID is required.");

    if (string.IsNullOrWhiteSpace(text))
        throw new ArgumentException(
            "Replacement document text cannot be empty.");

    if (string.IsNullOrWhiteSpace(fileName))
        throw new ArgumentException(
            "Replacement file name is required.");

    if (fileSizeBytes < 0)
        throw new ArgumentOutOfRangeException(
            nameof(fileSizeBytes));

    cancellationToken.ThrowIfCancellationRequested();

    DateTimeOffset updatedAt = DateTimeOffset.UtcNow;

    // Preserve the original creation time when available.
    // For legacy documents without metadata, leave it unknown
    // rather than inventing an original creation date.
    DateTimeOffset createdAt =
        originalCreatedAt ?? updatedAt;

    _logger.LogInformation(
        "Document replacement started. " +
        "OldDocumentId={OldDocumentId}, FileName={FileName}",
        oldDocumentId,
        fileName);

    DocumentIngestionResult newDocument =
        await IngestDocumentAsync(
            text: text,
            fileName: fileName,
            contentType: contentType,
            fileSizeBytes: fileSizeBytes,
            createdAt: createdAt,
            updatedAt: updatedAt,
            cancellationToken: cancellationToken);

    _logger.LogInformation(
        "Replacement document indexed. " +
        "OldDocumentId={OldDocumentId}, " +
        "NewDocumentId={NewDocumentId}, " +
        "FileName={FileName}, ChunkCount={ChunkCount}",
        oldDocumentId,
        newDocument.DocumentId,
        newDocument.FileName,
        newDocument.ChunkCount);

    try
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _vectorStore.DeleteDocumentAsync(
            oldDocumentId,
            cancellationToken);

        _logger.LogInformation(
            "Old document deleted after replacement. " +
            "OldDocumentId={OldDocumentId}, " +
            "NewDocumentId={NewDocumentId}",
            oldDocumentId,
            newDocument.DocumentId);
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "Document replacement failed after indexing the new version. " +
            "Attempting rollback. OldDocumentId={OldDocumentId}, " +
            "NewDocumentId={NewDocumentId}",
            oldDocumentId,
            newDocument.DocumentId);

        try
        {
            await _vectorStore.DeleteDocumentAsync(
                newDocument.DocumentId,
                CancellationToken.None);

            _logger.LogWarning(
                "Replacement rollback completed. " +
                "OldDocumentId={OldDocumentId}, NewDocumentId={NewDocumentId}",
                oldDocumentId,
                newDocument.DocumentId);
        }
        catch (Exception rollbackEx)
        {
            _logger.LogCritical(
                rollbackEx,
                "Replacement rollback failed. Both document versions may exist. " +
                "OldDocumentId={OldDocumentId}, NewDocumentId={NewDocumentId}",
                oldDocumentId,
                newDocument.DocumentId);
        }

        throw;
    }

    _logger.LogInformation(
        "Document replacement completed. " +
        "OldDocumentId={OldDocumentId}, NewDocumentId={NewDocumentId}",
        oldDocumentId,
        newDocument.DocumentId);

    return new DocumentUpdateResult
    {
        OldDocumentId = oldDocumentId,
        NewDocumentId = newDocument.DocumentId,
        FileName = newDocument.FileName,
        ChunkCount = newDocument.ChunkCount
    };
}

    private static ulong CreatePointId(
        string documentId,
        int chunkIndex)
    {
        string value = $"{documentId}:{chunkIndex}";

        byte[] bytes = Encoding.UTF8.GetBytes(value);
        byte[] hash = SHA256.HashData(bytes);

        return BitConverter.ToUInt64(hash, 0);
    }
}
