using System.Text;
using UglyToad.PdfPig;

namespace MyLlmApp.Api.DocumentExtractionService;

public class PdfDocumentTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string fileExtension)
    {
        return fileExtension.Equals(
            ".pdf",
            StringComparison.OrdinalIgnoreCase);
    }

    public Task<string> ExtractTextAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        StringBuilder text = new();

        using PdfDocument document =
            PdfDocument.Open(stream);

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(page.Text))
            {
                text.AppendLine(page.Text);
            }
        }

        return Task.FromResult(text.ToString());
    }
}