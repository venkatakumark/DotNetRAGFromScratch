
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
                        .GenerateEmbeddingAsync(chunk);

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
}
