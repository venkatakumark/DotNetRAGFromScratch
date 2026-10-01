namespace MyLlmApp.Core.Configuration;

public class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string Host { get; set; } =
        "localhost";

    public int Port { get; set; } =
        6334;

    public string CollectionName { get; set; } =
        "PolicyDocuments";

    public ulong VectorSize { get; set; } =
        768;
}