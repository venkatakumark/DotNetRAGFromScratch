namespace MyLlmApp.Core.RAG;

public class DocumentIngestionResult
{
    public string DocumentId { get; set; } = "";

    public string FileName { get; set; } = "";

    public int ChunkCount { get; set; }
}