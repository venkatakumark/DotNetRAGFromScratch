namespace MyLlmApp.Core.RAG;

public class IndexedDocument
{
    public string DocumentId { get; set; } = "";

    public string FileName { get; set; } = "";

    public string ContentType { get; set; } = "";

    public int ChunkCount { get; set; }
}