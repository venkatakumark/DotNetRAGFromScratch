namespace MyLlmApp.Api.DocumentExtractionService;

public class TxtDocumentTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string fileExtension)
    {
        return fileExtension.Equals(
            ".txt",
            StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ExtractTextAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        using StreamReader reader = new(
            stream,
            leaveOpen: true);

        return await reader.ReadToEndAsync(
            cancellationToken);
    }
}