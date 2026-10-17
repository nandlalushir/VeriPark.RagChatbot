using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using CommunityToolkit.VectorData.Qdrant;
using Qdrant.Client;
using VeriPark.RagChatbot.Api.Configuration;
using VeriPark.RagChatbot.Api.Domain;

namespace VeriPark.RagChatbot.Api.Retrieval;

/// <summary>
/// Qdrant implementation built on the CommunityToolkit.VectorData.Qdrant vector store connector.
/// This is the ONLY file that touches the vector store API, so if a future connector release renames a
/// method, this is the single place to adjust.
/// </summary>
public sealed class QdrantDocumentChunkRepository : IDocumentChunkRepository
{
    private readonly QdrantClient _client;
    private readonly string _collectionName;
    private readonly VectorStoreCollection<Guid, DocumentChunk> _collection;

    public QdrantDocumentChunkRepository(
        QdrantClient client,
        QdrantVectorStore vectorStore,
        IOptions<QdrantOptions> options)
    {
        _client = client;
        _collectionName = options.Value.CollectionName;
        _collection = vectorStore.GetCollection<Guid, DocumentChunk>(_collectionName);
    }

    public Task EnsureCollectionAsync(CancellationToken cancellationToken) =>
        _collection.EnsureCollectionExistsAsync(cancellationToken);

    public async Task RecreateCollectionAsync(CancellationToken cancellationToken)
    {
        await _collection.EnsureCollectionDeletedAsync(cancellationToken);
        await _collection.EnsureCollectionExistsAsync(cancellationToken);
    }

    public async Task<long> CountAsync(CancellationToken cancellationToken)
    {
        if (!await _collection.CollectionExistsAsync(cancellationToken))
        {
            return 0;
        }

        var count = await _client.CountAsync(_collectionName, cancellationToken: cancellationToken);
        return (long)count;
    }

    public Task UpsertAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken) =>
        _collection.UpsertAsync(chunks, cancellationToken);

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        ReadOnlyMemory<float> queryVector,
        int top,
        CancellationToken cancellationToken)
    {
        var results = new List<RetrievedChunk>(top);

        await foreach (var hit in _collection.SearchAsync(queryVector, top, cancellationToken: cancellationToken))
        {
            var record = hit.Record;
            results.Add(new RetrievedChunk(
                record.Id,
                record.DocumentName,
                record.PageNumber,
                record.ChunkIndex,
                record.Text,
                hit.Score ?? 0));
        }

        return results;
    }
}
