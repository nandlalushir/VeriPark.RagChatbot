using VeriPark.RagChatbot.Api.Contracts;

namespace VeriPark.RagChatbot.Api.Chat;

public interface IRagChatService
{
    Task<ChatResponse> AskAsync(string? conversationId, string message, CancellationToken cancellationToken);
}
