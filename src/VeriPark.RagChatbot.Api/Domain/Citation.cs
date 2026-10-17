namespace VeriPark.RagChatbot.Api.Domain;

/// <summary>Points to the place in the PDF that supports (part of) an answer.</summary>
public sealed record Citation(int PageNumber, string ChunkId, double Score, string Snippet);
