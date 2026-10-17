using VeriPark.RagChatbot.Api.Domain;

namespace VeriPark.RagChatbot.Api.Ingestion;

/// <summary>Splits page text into chunks that are small enough to embed and retrieve precisely.</summary>
public interface IDocumentChunker
{
    IReadOnlyList<TextChunk> Chunk(string documentName, IReadOnlyList<PageText> pages);
}
