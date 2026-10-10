namespace MyLlmApp.Core.RAG;

public class IndexedDocument
{
    public string DocumentId { get; set; } = "";

    public string FileName { get; set; } = "";

    public string ContentType { get; set; } = "";

    public int ChunkCount { get; set; }
      // Original uploaded file size in bytes.
    public long? FileSizeBytes { get; set; }

    // UTC timestamp when the document was first indexed.
    public DateTimeOffset? CreatedAt { get; set; }

    // UTC timestamp of the latest indexing/update.
    public DateTimeOffset? UpdatedAt { get; set; }
}