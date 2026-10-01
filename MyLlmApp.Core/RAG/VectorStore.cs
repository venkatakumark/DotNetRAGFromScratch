using Microsoft.Extensions.Options;
using MyLlmApp.Core.Configuration;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace MyLlmApp.Core.RAG;

public class VectorStore
{
    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;

    public VectorStore(
        IOptions<QdrantOptions> options)
    {
        _options = options.Value;

        _client = new QdrantClient(
            host: _options.Host,
            port: _options.Port);
    }

    public async Task CreateCollectionAsync()
    {
        bool exists =
            await _client.CollectionExistsAsync(
                _options.CollectionName);

        if (exists)
        {
            Console.WriteLine(
                $"Collection '{_options.CollectionName}' already exists.");

            return;
        }

        Console.WriteLine(
            $"Creating collection '{_options.CollectionName}'...");

        await _client.CreateCollectionAsync(
            collectionName: _options.CollectionName,
            vectorsConfig: new VectorParams
            {
                Size = _options.VectorSize,
                Distance = Distance.Cosine
            });

        Console.WriteLine(
            $"Collection '{_options.CollectionName}' created.");
    }

    public async Task AddChunkAsync(
        ulong id,
        float[] embedding,
        string text,
        string source,
        int chunkIndex)
    {
        PointStruct point =
            new()
            {
                Id = id,
                Vectors = embedding
            };

        point.Payload["text"] =
            new Value
            {
                StringValue = text
            };

        point.Payload["source"] =
            new Value
            {
                StringValue = source
            };

        point.Payload["chunkIndex"] =
            new Value
            {
                IntegerValue = chunkIndex
            };

        await _client.UpsertAsync(
            collectionName: _options.CollectionName,
            points: new[]
            {
                point
            });
    }

    public async Task<List<SearchResult>> SearchAsync(
        float[] queryEmbedding,
        int limit = 3)
    {
        IReadOnlyList<ScoredPoint> results =
            await _client.QueryAsync(
                collectionName: _options.CollectionName,
                query: queryEmbedding,
                limit: (ulong)limit);

        List<SearchResult> searchResults = [];

        foreach (ScoredPoint result in results)
        {
            string text = "";
            string source = "";
            int chunkIndex = 0;

            if (result.Payload.TryGetValue(
                    "text",
                    out Value? textValue))
            {
                text = textValue.StringValue ?? "";
            }

            if (result.Payload.TryGetValue(
                    "source",
                    out Value? sourceValue))
            {
                source = sourceValue.StringValue ?? "";
            }

            if (result.Payload.TryGetValue(
                    "chunkIndex",
                    out Value? chunkIndexValue))
            {
                chunkIndex =
                    (int)chunkIndexValue.IntegerValue;
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