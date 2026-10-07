using System.Collections.Concurrent;

namespace MyLlmApp.Core.RAG;

public class ConversationStore
{
    private readonly ConcurrentDictionary<
        string,
        ConversationSession> _conversations = new();

    public ConversationHistory GetOrCreate(
        string conversationId)
    {
        return GetOrCreateSession(
            conversationId).History;
    }

    public ConversationSession GetOrCreateSession(
        string conversationId)
    {
        return _conversations.GetOrAdd(
            conversationId,
            _ => new ConversationSession());
    }

    public bool Remove(
        string conversationId)
    {
        return _conversations.TryRemove(
            conversationId,
            out _);
    }

    public void Clear()
    {
        _conversations.Clear();
    }
}