namespace MyLlmApp.Core.RAG;

public class HybridReranker
{
    private const float VectorWeight = 0.80f;
    private const float KeywordWeight = 0.20f;

    private static readonly HashSet<string> StopWords =
    [
        "a", "an", "the",
        "is", "are", "was", "were",
        "what", "who", "when", "where", "why", "how",
        "to", "of", "for", "in", "on", "at",
        "and", "or", "with",
        "it", "its", "them", "they",
        "this", "that",
        "do", "does", "did",
        "can", "could", "will", "would",
        "be", "been", "being"
    ];

    public List<RerankedResult> Rerank(
        string question,
        List<SearchResult> results)
    {
        HashSet<string> questionWords =
            GetImportantWords(question);

        List<RerankedResult> rerankedResults = [];

        foreach (SearchResult result in results)
        {
            HashSet<string> documentWords =
                GetImportantWords(result.Text);

            float keywordScore =
                CalculateKeywordScore(
                    questionWords,
                    documentWords);

            float finalScore =
                (result.Score * VectorWeight) +
                (keywordScore * KeywordWeight);

            rerankedResults.Add(
                new RerankedResult
                {
                    SearchResult = result,
                    VectorScore = result.Score,
                    KeywordScore = keywordScore,
                    FinalScore = finalScore
                });
        }

        return rerankedResults
            .OrderByDescending(
                result => result.FinalScore)
            .ToList();
    }

    private static float CalculateKeywordScore(
        HashSet<string> questionWords,
        HashSet<string> documentWords)
    {
        if (questionWords.Count == 0)
        {
            return 0;
        }

        int matchingWords =
            questionWords.Count(
                word =>
                    documentWords.Contains(word));

        return (float)matchingWords /
               questionWords.Count;
    }

    private static HashSet<string> GetImportantWords(
        string text)
    {
        char[] separators =
        [
            ' ', '\t', '\r', '\n',
            '.', ',', '?', '!',
            ':', ';',
            '(', ')',
            '[', ']',
            '"', '\''
        ];

        return text
            .ToLowerInvariant()
            .Split(
                separators,
                StringSplitOptions.RemoveEmptyEntries)
            .Where(
                word =>
                    word.Length > 1 &&
                    !StopWords.Contains(word))
            .ToHashSet();
    }
}

public class RerankedResult
{
    public SearchResult SearchResult { get; set; } =
        new();

    public float VectorScore { get; set; }

    public float KeywordScore { get; set; }

    public float FinalScore { get; set; }
}