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
public async Task AddChunkAsync(
    ulong id,
    float[] embedding,
    string text,
    string source,
    int chunkIndex,
    string documentId,
    string contentType,
    string documentHash,
    CancellationToken cancellationToken)
{
    PointStruct point = new()
    {
        Id = id,
        Vectors = embedding
    };

    point.Payload["text"] =
        new Value { StringValue = text };

    point.Payload["source"] =
        new Value { StringValue = source };

    point.Payload["chunkIndex"] =
        new Value { IntegerValue = chunkIndex };

    point.Payload["documentId"] =
        new Value { StringValue = documentId };

    point.Payload["contentType"] =
        new Value { StringValue = contentType };

    point.Payload["documentHash"] =
        new Value { StringValue = documentHash };

    await _client.UpsertAsync(
        collectionName: _options.CollectionName,
        points: new[] { point },
        cancellationToken: cancellationToken);
}
    // ------------------------------------------------
    // Existing SearchAsync
    //
    // Preserved so existing callers continue working.
    // ------------------------------------------------

    public async Task<List<SearchResult>> SearchAsync(
        float[] queryEmbedding,
        int limit = 3)
    {
        return await SearchAsync(
            queryEmbedding,
            limit,
            CancellationToken.None);
    }

    public async Task<IndexedDocument?> FindDocumentByFileNameAsync(
    string fileName,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(fileName))
    {
        throw new ArgumentException(
            "File name cannot be empty.",
            nameof(fileName));
    }

    Filter filter = new()
    {
        Must =
        {
            new Condition
            {
                Field = new FieldCondition
                {
                    Key = "source",
                    Match = new Match
                    {
                        Keyword = fileName
                    }
                }
            }
        }
    };

    PointId? offset = null;

    do
    {
        var response =
            await _client.ScrollAsync(
                collectionName: _options.CollectionName,
                filter: filter,
                limit: 100,
                offset: offset,
                payloadSelector: true,
                vectorsSelector: false,
                cancellationToken: cancellationToken);

        foreach (RetrievedPoint point in response.Result)
        {
            // Ignore legacy documents because they do not
            // participate in our managed document lifecycle.
            if (!point.Payload.TryGetValue(
                    "documentId",
                    out Value? documentIdValue))
            {
                continue;
            }

            string documentId =
                documentIdValue.StringValue ?? "";

            if (string.IsNullOrWhiteSpace(documentId))
            {
                continue;
            }

            string contentType = "";

            if (point.Payload.TryGetValue(
                    "contentType",
                    out Value? contentTypeValue))
            {
                contentType =
                    contentTypeValue.StringValue ?? "";
            }

            int chunkCount =
                response.Result.Count(
                    p =>
                        p.Payload.TryGetValue(
                            "documentId",
                            out Value? value)
                        &&
                        value.StringValue == documentId);

            return new IndexedDocument
            {
                DocumentId = documentId,
                FileName = fileName,
                ContentType = contentType,
                ChunkCount = chunkCount
            };
        }

        offset = response.NextPageOffset;

    } while (offset is not null);

    return null;
}


public async Task AddChunkAsync(
    ulong id,
    float[] embedding,
    string text,
    string source,
    int chunkIndex,
    string documentId,
    string contentType,
    string documentHash,
    long fileSizeBytes,
    DateTimeOffset createdAt,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken)
{
    if (fileSizeBytes < 0)
        throw new ArgumentOutOfRangeException(
            nameof(fileSizeBytes));

    PointStruct point = new()
    {
        Id = id,
        Vectors = embedding
    };

    point.Payload["text"] = new Value
    {
        StringValue = text
    };

    point.Payload["source"] = new Value
    {
        StringValue = source
    };

    point.Payload["chunkIndex"] = new Value
    {
        IntegerValue = chunkIndex
    };

    point.Payload["documentId"] = new Value
    {
        StringValue = documentId
    };

    point.Payload["contentType"] = new Value
    {
        StringValue = contentType
    };

    point.Payload["documentHash"] = new Value
    {
        StringValue = documentHash
    };

    point.Payload["fileSizeBytes"] = new Value
    {
        IntegerValue = fileSizeBytes
    };

    point.Payload["createdAt"] = new Value
    {
        StringValue = createdAt.ToUniversalTime().ToString("O")
    };

    point.Payload["updatedAt"] = new Value
    {
        StringValue = updatedAt.ToUniversalTime().ToString("O")
    };

    await _client.UpsertAsync(
        collectionName: _options.CollectionName,
        points: new[] { point },
        cancellationToken: cancellationToken);
}

    // ------------------------------------------------
    // Cancellation-aware SearchAsync
    // ------------------------------------------------

    public async Task<List<SearchResult>> SearchAsync(
        float[] queryEmbedding,
        int limit,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ScoredPoint> results =
            await _client.QueryAsync(
                collectionName: _options.CollectionName,
                query: queryEmbedding,
                limit: (ulong)limit,
                cancellationToken: cancellationToken);

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
                text =
                    textValue.StringValue ?? "";
            }

            if (result.Payload.TryGetValue(
                    "source",
                    out Value? sourceValue))
            {
                source =
                    sourceValue.StringValue ?? "";
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

public async Task<List<IndexedDocument>> GetDocumentsAsync(
    CancellationToken cancellationToken)
{
    Dictionary<string, IndexedDocument> documents = new();

    PointId? offset = null;

    do
    {
        var response = await _client.ScrollAsync(
            collectionName: _options.CollectionName,
            limit: 100,
            offset: offset,
            payloadSelector: true,
            vectorsSelector: false,
            cancellationToken: cancellationToken);

        foreach (RetrievedPoint point in response.Result)
        {
            if (!point.Payload.TryGetValue(
                    "documentId",
                    out Value? documentIdValue))
            {
                // Old manually-ingested documents don't
                // contain documentId, so leave them alone.
                continue;
            }

            string documentId =
                documentIdValue.StringValue ?? "";

            if (string.IsNullOrWhiteSpace(documentId))
            {
                continue;
            }

            string fileName = "";
            string contentType = "";

            if (point.Payload.TryGetValue(
                    "source",
                    out Value? sourceValue))
            {
                fileName = sourceValue.StringValue ?? "";
            }

            if (point.Payload.TryGetValue(
                    "contentType",
                    out Value? contentTypeValue))
            {
                contentType = contentTypeValue.StringValue ?? "";
            }

            // Read file size metadata.
            long? fileSizeBytes = null;

            if (point.Payload.TryGetValue(
                    "fileSizeBytes",
                    out Value? sizeValue))
            {
                fileSizeBytes = sizeValue.IntegerValue;
            }

            // Read creation timestamp.
            DateTimeOffset? createdAt = null;

            if (point.Payload.TryGetValue(
                    "createdAt",
                    out Value? createdValue) &&
                DateTimeOffset.TryParse(
                    createdValue.StringValue,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTimeOffset parsedCreatedAt))
            {
                createdAt = parsedCreatedAt;
            }

            // Read last updated timestamp.
            DateTimeOffset? updatedAt = null;

            if (point.Payload.TryGetValue(
                    "updatedAt",
                    out Value? updatedValue) &&
                DateTimeOffset.TryParse(
                    updatedValue.StringValue,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTimeOffset parsedUpdatedAt))
            {
                updatedAt = parsedUpdatedAt;
            }

            if (!documents.TryGetValue(
                    documentId,
                    out IndexedDocument? document))
            {
                document = new IndexedDocument
                {
                    DocumentId = documentId,
                    FileName = fileName,
                    ContentType = contentType,
                    ChunkCount = 0,
                    FileSizeBytes = fileSizeBytes,
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt
                };

                documents[documentId] = document;
            }
            else
            {
                // Populate missing metadata if another
                // chunk contains it.
                document.FileSizeBytes ??= fileSizeBytes;
                document.CreatedAt ??= createdAt;
                document.UpdatedAt ??= updatedAt;
            }

            document.ChunkCount++;
        }

        offset = response.NextPageOffset;

    } while (offset is not null);

    return documents.Values
        .OrderBy(d => d.FileName)
        .ToList();
}

    public async Task DeleteDocumentAsync(
    string documentId,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(documentId))
    {
        throw new ArgumentException(
            "Document ID cannot be empty.",
            nameof(documentId));
    }

    Filter filter =
        new()
        {
            Must =
            {
                new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "documentId",
                        Match = new Match
                        {
                            Keyword = documentId
                        }
                    }
                }
            }
        };

    await _client.DeleteAsync(
        collectionName: _options.CollectionName,
        filter: filter,
        cancellationToken: cancellationToken);
}
    public async Task<IndexedDocument?> FindDocumentByHashAsync(
    string documentHash,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(documentHash))
    {
        throw new ArgumentException(
            "Document hash cannot be empty.",
            nameof(documentHash));
    }

    Filter filter = new()
    {
        Must =
        {
            new Condition
            {
                Field = new FieldCondition
                {
                    Key = "documentHash",
                    Match = new Match
                    {
                        Keyword = documentHash
                    }
                }
            }
        }
    };

    var response = await _client.ScrollAsync(
        collectionName: _options.CollectionName,
        filter: filter,
        limit: 1,
        payloadSelector: true,
        vectorsSelector: false,
        cancellationToken: cancellationToken);

    RetrievedPoint? point =
        response.Result.FirstOrDefault();

    if (point is null)
    {
        return null;
    }

    string documentId = "";
    string fileName = "";
    string contentType = "";

    if (point.Payload.TryGetValue(
            "documentId",
            out Value? documentIdValue))
    {
        documentId =
            documentIdValue.StringValue ?? "";
    }

    if (point.Payload.TryGetValue(
            "source",
            out Value? sourceValue))
    {
        fileName =
            sourceValue.StringValue ?? "";
    }

    if (point.Payload.TryGetValue(
            "contentType",
            out Value? contentTypeValue))
    {
        contentType =
            contentTypeValue.StringValue ?? "";
    }

    return new IndexedDocument
    {
        DocumentId = documentId,
        FileName = fileName,
        ContentType = contentType,
        ChunkCount = 0
    };
}
}

public class SearchResult
{
    public float Score { get; set; }

    public string Text { get; set; } = "";

    public string Source { get; set; } = "";

    public int ChunkIndex { get; set; }
}