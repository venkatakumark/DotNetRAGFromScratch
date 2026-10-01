using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace MyLlmApp.Core.RAG;

public class VectorStore
{
    private readonly QdrantClient _client;

    private const string CollectionName =
        "PolicyDocuments";

    private const int VectorDimension = 768;

    public VectorStore()
    {
        _client = new QdrantClient(
            host: "localhost",
            port: 6334);
    }

    public async Task CreateCollectionAsync()
    {
        bool exists =
            await _client.CollectionExistsAsync(
                CollectionName);

        if (exists)
        {
            Console.WriteLine(
                $"Collection '{CollectionName}' already exists.");

            return;
        }

        await _client.CreateCollectionAsync(
            CollectionName,
            new VectorParams
            {
                Size = VectorDimension,
                Distance = Distance.Cosine
            });

        Console.WriteLine(
            $"Collection '{CollectionName}' created.");
    }

    public async Task AddChunkAsync(
        ulong id,
        float[] embedding,
        string text,
        string source,
        int chunkIndex)
    {
        if (embedding.Length != VectorDimension)
        {
            throw new ArgumentException(
                $"Expected {VectorDimension} dimensions " +
                $"but received {embedding.Length}.");
        }

        var point = new PointStruct
        {
            Id = id,
            Vectors = embedding,

            Payload =
            {
                ["text"] = text,
                ["source"] = source,
                ["chunkIndex"] = chunkIndex
            }
        };

        await _client.UpsertAsync(
            CollectionName,
            new[] { point });

        Console.WriteLine(
            $"Stored chunk {chunkIndex} " +
            $"from '{source}' with ID {id}.");
    }

    public async Task<List<SearchResult>> SearchAsync(
        float[] queryEmbedding,
        int limit = 3)
    {
        if (queryEmbedding.Length != VectorDimension)
        {
            throw new ArgumentException(
                $"Expected {VectorDimension} dimensions " +
                $"but received {queryEmbedding.Length}.");
        }

        var results = await _client.QueryAsync(
            CollectionName,
            queryEmbedding,
            limit: (ulong)limit,
            payloadSelector: true);

        List<SearchResult> searchResults = [];

        foreach (var result in results)
        {
            string text = "";

            string source = "";

            int chunkIndex = -1;

            if (result.Payload.TryGetValue(
                "text",
                out Value textValue))
            {
                text = textValue.StringValue;
            }

            if (result.Payload.TryGetValue(
                "source",
                out Value sourceValue))
            {
                source = sourceValue.StringValue;
            }

            if (result.Payload.TryGetValue(
                "chunkIndex",
                out Value chunkValue))
            {
                chunkIndex =
                    (int)chunkValue.IntegerValue;
            }

            searchResults.Add(
                new SearchResult
                {
                    Score = result.Score,
                    Text = text,
                    Source = source,
                    ChunkIndex = chunkIndex
                });
        }

        return searchResults;
    }
}

public class SearchResult
{
    public float Score { get; set; }

    public string Text { get; set; } = "";

    public string Source { get; set; } = "";

    public int ChunkIndex { get; set; }
}