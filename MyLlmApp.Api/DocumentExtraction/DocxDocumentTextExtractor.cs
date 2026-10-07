using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace MyLlmApp.Api.DocumentExtractionService;

public class DocxDocumentTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string fileExtension)
    {
        return fileExtension.Equals(
            ".docx",
            StringComparison.OrdinalIgnoreCase);
    }

    public Task<string> ExtractTextAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        StringBuilder text = new();

        using WordprocessingDocument document =
            WordprocessingDocument.Open(
                stream,
                false);

        Body? body =
            document.MainDocumentPart?
                .Document?
                .Body;

        if (body is null)
        {
            return Task.FromResult(string.Empty);
        }

        foreach (Paragraph paragraph in
                 body.Descendants<Paragraph>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            string paragraphText =
                paragraph.InnerText;

            if (!string.IsNullOrWhiteSpace(paragraphText))
            {
                text.AppendLine(paragraphText);
            }
        }

        return Task.FromResult(text.ToString());
    }
}