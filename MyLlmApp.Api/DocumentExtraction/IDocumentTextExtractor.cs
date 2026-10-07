namespace MyLlmApp.Api.DocumentExtractionService;

public interface IDocumentTextExtractor
{
    bool CanHandle(string fileExtension);

    Task<string> ExtractTextAsync(
        Stream stream,
        CancellationToken cancellationToken);
}