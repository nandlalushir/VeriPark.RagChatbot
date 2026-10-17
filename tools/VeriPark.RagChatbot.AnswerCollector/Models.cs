namespace VeriPark.RagChatbot.AnswerCollector;

internal sealed record ChatRequest(string ConversationId, string Message);

internal sealed record CitationDto(int PageNumber, string ChunkId, double Score, string Snippet);

internal sealed record ChatResponse(
    string ConversationId,
    string Answer,
    List<CitationDto> Citations,
    string StandaloneQuestion);

internal sealed record HealthResponse(string Status, bool IsReady, int ChunkCount, string? Error);
