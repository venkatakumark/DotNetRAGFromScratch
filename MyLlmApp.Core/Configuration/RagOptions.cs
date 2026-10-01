namespace MyLlmApp.Core.Configuration;

public class RagOptions
{
    public const string SectionName = "Rag";

    public int TopK { get; set; } = 5;

    public float MinimumScore { get; set; } =
        0.65f;

    public float MaximumScoreGap { get; set; } =
        0.10f;

    public int FinalContextCount { get; set; } =
        3;
}