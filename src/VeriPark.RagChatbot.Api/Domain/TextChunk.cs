namespace VeriPark.RagChatbot.Api.Domain;

/// <summary>A piece of text produced by the chunker, before it has an embedding.</summary>
public sealed record TextChunk(string DocumentName, int PageNumber, int ChunkIndex, string Text);
