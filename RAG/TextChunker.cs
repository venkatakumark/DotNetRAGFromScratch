using System.Text.RegularExpressions;

namespace MyLlmApp.RAG;

public class TextChunker
{
    private readonly int _chunkSize;
    private readonly int _overlapSentences;

    public TextChunker(
        int chunkSize = 500,
        int overlapSentences = 1)
    {
        if (chunkSize <= 0)
            throw new ArgumentException(
                "Chunk size must be greater than zero.",
                nameof(chunkSize));

        if (overlapSentences < 0)
            throw new ArgumentException(
                "Overlap sentences cannot be negative.",
                nameof(overlapSentences));

        _chunkSize = chunkSize;
        _overlapSentences = overlapSentences;
    }

    public List<string> SplitText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        List<string> sentences =
            SplitIntoSentences(text.Trim());

        List<string> chunks = [];

        int start = 0;

        while (start < sentences.Count)
        {
            List<string> currentSentences = [];
            int currentLength = 0;

            int index = start;

            while (index < sentences.Count)
            {
                string sentence = sentences[index];

                int newLength =
                    currentSentences.Count == 0
                        ? sentence.Length
                        : currentLength + 1 + sentence.Length;

                if (newLength > _chunkSize &&
                    currentSentences.Count > 0)
                {
                    break;
                }

                currentSentences.Add(sentence);
                currentLength = newLength;

                index++;
            }

            chunks.Add(
                string.Join(" ", currentSentences).Trim());

            if (index >= sentences.Count)
                break;

            /*
             * Overlap complete sentences.
             *
             * Example:
             *
             * Chunk 0:
             * Sentence 1
             * Sentence 2
             *
             * Chunk 1:
             * Sentence 2
             * Sentence 3
             */

            int nextStart =
                Math.Max(
                    start + 1,
                    index - _overlapSentences);

            start = nextStart;
        }

        return chunks;
    }

    private static List<string> SplitIntoSentences(
        string text)
    {
        string[] sentences =
            Regex.Split(
                text,
                @"(?<=[.!?])\s+");

        return sentences
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();
    }
}