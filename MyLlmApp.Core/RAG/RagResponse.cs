namespace MyLlmApp.Core.RAG;

public class RagResponse
{
    public string Answer { get; set; } = "";

    public List<RagSource> Sources { get; set; } = [];
}

public class RagSource
{
    public string Source { get; set; } = "";

    public int ChunkIndex { get; set; }

    public float Score { get; set; }
}