
namespace MyLlmApp.Core.RAG;

public class DocumentIngestionService
{
    private readonly TextChunker _chunker;
    private readonly EmbeddingService _embeddingService;
    private readonly VectorStore _vectorStore;

    public DocumentIngestionService(
        TextChunker chunker,
        EmbeddingService embeddingService,
        VectorStore vectorStore)
    {
        _chunker = chunker;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
    }

    public async Task IngestDocumentsAsync(
        List<(string Text, string Source)> documents)
    {
        // Make sure the Qdrant collection exists
        await _vectorStore.CreateCollectionAsync();

        ulong nextId = 1000;

        foreach (var document in documents)
        {
            Console.WriteLine();
            Console.WriteLine("======================================");
            Console.WriteLine($"Ingesting: {document.Source}");
            Console.WriteLine("======================================");

            List<string> chunks =
                _chunker.SplitText(document.Text);

            Console.WriteLine(
                $"Chunks created: {chunks.Count}");

            for (int i = 0; i < chunks.Count; i++)
            {
                string chunk = chunks[i];

                Console.WriteLine(
                    $"Creating embedding for chunk {i}...");

                float[] embedding =
                    await _embeddingService
                        .CreateEmbeddingAsync(chunk,CancellationToken.None);

                await _vectorStore.AddChunkAsync(
                    id: nextId,
                    embedding: embedding,
                    text: chunk,
                    source: document.Source,
                    chunkIndex: i);

                Console.WriteLine(
                    $"Stored in Qdrant with ID: {nextId}");

                nextId++;
            }

            Console.WriteLine(
                $"Completed: {document.Source}");
        }

        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("ALL DOCUMENTS INGESTED");
        Console.WriteLine("======================================");
    }
   public async Task<DocumentIngestionResult> IngestDocumentAsync(
    string text,
    string fileName,
    string contentType,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(text))
    {
        throw new ArgumentException(
            "Document text cannot be empty.",
            nameof(text));
    }

    if (string.IsNullOrWhiteSpace(fileName))
    {
        throw new ArgumentException(
            "File name cannot be empty.",
            nameof(fileName));
    }

    await _vectorStore.CreateCollectionAsync();

    // Calculate SHA-256 from document content.
    byte[] contentBytes =
        System.Text.Encoding.UTF8.GetBytes(text);

    byte[] hashBytes =
        System.Security.Cryptography.SHA256.HashData(
            contentBytes);

    string documentHash =
        Convert.ToHexString(hashBytes)
            .ToLowerInvariant();

    // Check whether the same content is already indexed.
    IndexedDocument? existingDocument =
        await _vectorStore.FindDocumentByHashAsync(
            documentHash,
            cancellationToken);

    if (existingDocument is not null)
    {
        throw new InvalidOperationException(
            $"Document already indexed as '{existingDocument.FileName}' " +
            $"with documentId '{existingDocument.DocumentId}'.");
    }

    string documentId =
        Guid.NewGuid().ToString("N");

    List<string> chunks =
        _chunker.SplitText(text);

    if (chunks.Count == 0)
    {
        throw new InvalidOperationException(
            "No text chunks were created from the document.");
    }

    for (int i = 0; i < chunks.Count; i++)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string chunk = chunks[i];

        float[] embedding =
            await _embeddingService.CreateEmbeddingAsync(
                chunk,
                cancellationToken);

        ulong pointId =
            CreatePointId(documentId, i);

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

    return new DocumentIngestionResult
    {
        DocumentId = documentId,
        FileName = fileName,
        ChunkCount = chunks.Count
    };
}
private static ulong CreatePointId(
    string documentId,
    int chunkIndex)
{
    string value =
        $"{documentId}:{chunkIndex}";

    byte[] bytes =
        System.Text.Encoding.UTF8.GetBytes(value);

    byte[] hash =
        System.Security.Cryptography.SHA256.HashData(
            bytes);

    return BitConverter.ToUInt64(
        hash,
        0);
}
}
