using VeriPark.RagChatbot.Api.Domain;

namespace VeriPark.RagChatbot.Api.Contracts;

/// <param name="ConversationId">Leave empty to start a new conversation; send the returned id for follow-up questions.</param>
/// <param name="Message">The user's question.</param>
public sealed record ChatRequest(string? ConversationId, string Message);

/// <param name="ConversationId">Id to reuse for the next question.</param>
/// <param name="Answer">The answer generated from the retrieved document chunks.</param>
/// <param name="Citations">Document passages that were referenced in the answer.</param>
/// <param name="StandaloneQuestion">The question after follow-up references were resolved (used for retrieval).</param>
public sealed record ChatResponse(
    string ConversationId,
    string Answer,
    IReadOnlyList<Citation> Citations,
    string StandaloneQuestion);

public sealed record ConversationHistoryResponse(string ConversationId, IReadOnlyList<ConversationTurn> Turns);

public sealed record IngestResponse(int TotalChunks, bool Skipped, string Message);
