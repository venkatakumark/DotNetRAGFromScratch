namespace MyLlmApp.RAG;

public class DocumentLoader
{
    public List<(string Text, string Source)> LoadDocuments(
        string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException(
                $"Document folder not found: {folderPath}");
        }

        string[] files =
            Directory.GetFiles(
                folderPath,
                "*.txt",
                SearchOption.TopDirectoryOnly);

        List<(string Text, string Source)> documents = [];

        foreach (string file in files)
        {
            string text =
                File.ReadAllText(file);

            if (string.IsNullOrWhiteSpace(text))
                continue;

            string source =
                Path.GetFileName(file);

            documents.Add((text, source));
        }

        return documents;
    }
}