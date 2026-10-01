namespace MyLlmApp.RAG;

public class ConversationHistory
{
    private readonly List<ConversationMessage> _messages = [];

    public IReadOnlyList<ConversationMessage> Messages
        => _messages;

    public void AddUserMessage(string message)
    {
        _messages.Add(
            new ConversationMessage
            {
                Role = "User",
                Content = message
            });
    }

    public void AddAssistantMessage(string message)
    {
        _messages.Add(
            new ConversationMessage
            {
                Role = "Assistant",
                Content = message
            });
    }

    public void Clear()
    {
        _messages.Clear();
    }

    public bool HasHistory =>
        _messages.Count > 0;
}