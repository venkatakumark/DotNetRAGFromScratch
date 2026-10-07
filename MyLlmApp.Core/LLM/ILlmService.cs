namespace MyLlmApp.Core.LLM;

public interface ILlmService
{
     Task<string> GenerateAsync(string prompt);
    Task<string> GenerateAsync(
        string prompt, CancellationToken cancellationToken );
}