using VeriPark.RagChatbot.Api.Domain;

namespace VeriPark.RagChatbot.Api.Chat;

/// <summary>Turns a follow-up message into a question that makes sense without the chat history.</summary>
public interface IQueryRewriter
{
    Task<string> RewriteAsync(
        string question,
        IReadOnlyList<ConversationTurn> history,
        CancellationToken cancellationToken);
}
