namespace MyLlmApp.Core.RAG;

public class DuplicateDocumentException : Exception
{
    public string DocumentId { get; }
    public string FileName { get; }

    public DuplicateDocumentException(
        string documentId,
        string fileName)
        : base(
            $"Document already indexed as '{fileName}' " +
            $"with documentId '{documentId}'.")
    {
        DocumentId = documentId;
        FileName = fileName;
    }
}