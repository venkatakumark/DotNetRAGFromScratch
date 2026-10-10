namespace MyLlmApp.Core.RAG;

public class DocumentUpdateResult
{
    public string OldDocumentId { get; set; } = "";
    public string NewDocumentId { get; set; } = "";
    public string FileName { get; set; } = "";
    public int ChunkCount { get; set; }
}