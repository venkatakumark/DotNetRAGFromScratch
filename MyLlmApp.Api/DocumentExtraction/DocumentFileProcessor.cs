using UglyToad.PdfPig.Core;

namespace MyLlmApp.Api.DocumentExtractionService;

public class DocumentFileProcessor
{
    private const long MaxFileSize =
        5 * 1024 * 1024;

    private readonly IEnumerable<IDocumentTextExtractor>
        _extractors;

    public DocumentFileProcessor(
        IEnumerable<IDocumentTextExtractor> extractors)
    {
        _extractors = extractors;
    }

    public async Task<DocumentFileProcessResult> ProcessAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException(
                "A file is required.");
        }

        if (file.Length > MaxFileSize)
        {
            throw new ArgumentException(
                "File size cannot exceed 5 MB.");
        }

        string extension =
            Path.GetExtension(file.FileName);

        IDocumentTextExtractor? extractor =
            _extractors.FirstOrDefault(
                e => e.CanHandle(extension));

        if (extractor is null)
        {
            throw new ArgumentException(
                $"Unsupported file type '{extension}'. " +
                "Currently supported types are .txt, .pdf and .docx.");
        }

        string text;

        try
        {
            using Stream stream =
                file.OpenReadStream();

            text = await extractor.ExtractTextAsync(
                stream,
                cancellationToken);
        }
        catch (PdfDocumentFormatException ex)
        {
            throw new ArgumentException(
                "The uploaded file is not a valid PDF or the PDF is corrupted.",
                ex);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "No readable text could be extracted from the document.");
        }

        return new DocumentFileProcessResult
        {
            Text = text,
            FileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            FileSizeBytes = file.Length
        };
    }
}