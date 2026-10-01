namespace MyLlmApp.Core.Configuration;

public class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } =
        "http://localhost:11434";

    public string EmbeddingModel { get; set; } =
        "nomic-embed-text";

    public string QueryRewriteModel { get; set; } =
        "qwen3:0.6b";

    public string ChatModel { get; set; } =
        "granite4.2:latest";
}