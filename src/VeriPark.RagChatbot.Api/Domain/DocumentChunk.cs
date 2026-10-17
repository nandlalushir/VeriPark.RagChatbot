using Microsoft.Extensions.VectorData;

namespace VeriPark.RagChatbot.Api.Domain;

/// <summary>
/// One chunk of the PDF as it is stored in Qdrant.
/// The attributes tell the Semantic Kernel vector store connector how to map the class to a Qdrant point.
/// </summary>
public sealed class DocumentChunk
{
    /// <summary>
    /// Size of the embedding vector. 1536 matches text-embedding-3-small and text-embedding-ada-002.
    /// If you deploy text-embedding-3-large (3072), change this value and re-ingest.
    /// </summary>
    public const int EmbeddingDimensions = 1536;

    [VectorStoreKey]
    public Guid Id { get; set; }

    [VectorStoreData]
    public string DocumentName { get; set; } = string.Empty;

    [VectorStoreData]
    public int PageNumber { get; set; }

    [VectorStoreData]
    public int ChunkIndex { get; set; }

    [VectorStoreData]
    public string Text { get; set; } = string.Empty;

    [VectorStoreVector(EmbeddingDimensions, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}
