namespace MyLlmApp.Core.RAG;

public class EvaluationTestCase
{
    public string Question { get; set; } = "";

    public List<string> ExpectedKeywords { get; set; } = [];

    public bool ExpectNoAnswer { get; set; }
}