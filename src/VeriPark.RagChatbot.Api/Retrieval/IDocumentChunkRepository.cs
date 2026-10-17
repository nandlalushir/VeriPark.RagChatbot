using VeriPark.RagChatbot.Api.Domain;

namespace VeriPark.RagChatbot.Api.Retrieval;

/// <summary>Storage abstraction for document chunks. The only implementation talks to Qdrant.</summary>
public interface IDocumentChunkRepository
{
    Task EnsureCollectionAsync(CancellationToken cancellationToken);

    Task RecreateCollectionAsync(CancellationToken cancellationToken);

    Task<long> CountAsync(CancellationToken cancellationToken);

    Task UpsertAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken);

    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        ReadOnlyMemory<float> queryVector,
        int top,
        CancellationToken cancellationToken);
}
