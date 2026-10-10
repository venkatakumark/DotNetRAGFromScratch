namespace MyLlmApp.Api.DocumentExtractionService;

public class DocumentFileProcessResult
{
    public string Text { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
     // Actual uploaded file size, not extracted text length.
    public long FileSizeBytes { get; set; }
}