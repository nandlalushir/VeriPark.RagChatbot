namespace VeriPark.RagChatbot.Api.Domain;

/// <summary>A chunk returned by the vector search together with its similarity score.</summary>
public sealed record RetrievedChunk(
    Guid Id,
    string DocumentName,
    int PageNumber,
    int ChunkIndex,
    string Text,
    double Score);
