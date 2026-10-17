using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using VeriPark.RagChatbot.Api.Configuration;
using VeriPark.RagChatbot.Api.Domain;

namespace VeriPark.RagChatbot.Api.Retrieval;

/// <summary>
/// Turns the question into a vector with the Azure OpenAI embedding model and asks Qdrant for the closest chunks.
/// Chunks below the configured similarity score are dropped so weak matches never reach the prompt.
/// </summary>
public sealed class DocumentRetriever : IDocumentRetriever
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IDocumentChunkRepository _repository;
    private readonly RagOptions _options;
    private readonly ILogger<DocumentRetriever> _logger;

    public DocumentRetriever(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IDocumentChunkRepository repository,
        IOptions<RagOptions> options,
        ILogger<DocumentRetriever> logger)
    {
        _embeddingGenerator = embeddingGenerator;
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string query, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var embeddings = await _embeddingGenerator.GenerateAsync([query], cancellationToken: cancellationToken);
        var hits = await _repository.SearchAsync(embeddings[0].Vector, _options.TopK, cancellationToken);

        var relevant = hits
            .Where(hit => hit.Score >= _options.MinRelevanceScore)
            .OrderByDescending(hit => hit.Score)
            .ToList();

        _logger.LogDebug(
            "Retrieval for '{Query}': {Returned} hits, {Kept} kept (min score {MinScore}). Pages: {Pages}",
            query, hits.Count, relevant.Count, _options.MinRelevanceScore,
            string.Join(", ", relevant.Select(r => $"p.{r.PageNumber}:{r.Score:F2}")));

        return relevant;
    }
}
