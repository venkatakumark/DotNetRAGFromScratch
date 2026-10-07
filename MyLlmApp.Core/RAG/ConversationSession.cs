namespace MyLlmApp.Core.RAG;

public class ConversationSession
{
    public ConversationHistory History { get; } = new();

    public SemaphoreSlim Lock { get; } =
        new(1, 1);
}
