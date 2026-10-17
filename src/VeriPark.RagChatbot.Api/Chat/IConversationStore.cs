using VeriPark.RagChatbot.Api.Domain;

namespace VeriPark.RagChatbot.Api.Chat;

/// <summary>Keeps the chat history of every conversation in memory.</summary>
public interface IConversationStore
{
    /// <summary>Returns the conversation with this id, or creates it. A null/empty id creates a new conversation.</summary>
    ConversationSession GetOrCreate(string? conversationId);

    ConversationSession? Find(string conversationId);

    bool Remove(string conversationId);
}
