using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using VeriPark.RagChatbot.Api.Configuration;
using VeriPark.RagChatbot.Api.Domain;
using VeriPark.RagChatbot.Api.Retrieval;

namespace VeriPark.RagChatbot.Api.Ingestion;

public sealed class DocumentIngestionService : IDocumentIngestionService
{
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IDocumentChunker _chunker;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IDocumentChunkRepository _repository;
    private readonly IHostEnvironment _environment;
    private readonly RagOptions _options;
    private readonly ILogger<DocumentIngestionService> _logger;

    // Only one ingestion may run at a time (startup job vs. POST /api/ingest).
    private readonly SemaphoreSlim _gate = new(1, 1);

    public DocumentIngestionService(
        IPdfTextExtractor pdfTextExtractor,
        IDocumentChunker chunker,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IDocumentChunkRepository repository,
        IHostEnvironment environment,
        IOptions<RagOptions> options,
        ILogger<DocumentIngestionService> logger)
    {
        _pdfTextExtractor = pdfTextExtractor;
        _chunker = chunker;
        _embeddingGenerator = embeddingGenerator;
        _repository = repository;
        _environment = environment;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IngestionResult> IngestAsync(bool force, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var pdfPath = ResolvePdfPath();
            _logger.LogInformation("Starting ingestion of '{PdfPath}' (force: {Force}).", pdfPath, force);

            // Extraction and chunking are cheap and local, so they always run.
            // That lets us compare the expected chunk count with what Qdrant already holds.
            var pages = _pdfTextExtractor.Extract(pdfPath);
            var chunks = _chunker.Chunk(_options.DocumentTitle, pages);
            if (chunks.Count == 0)
            {
                throw new InvalidOperationException("No text could be extracted from the PDF. Is it a scanned document?");
            }

            _logger.LogInformation("Created {ChunkCount} chunks from {PageCount} pages.", chunks.Count, pages.Count);

            await _repository.EnsureCollectionAsync(cancellationToken);
            var existingCount = await _repository.CountAsync(cancellationToken);

            if (!force && existingCount == chunks.Count)
            {
                _logger.LogInformation("Qdrant already contains {Count} chunks. Skipping embedding.", existingCount);
                return new IngestionResult(chunks.Count, Skipped: true);
            }

            if (existingCount > 0 || force)
            {
                _logger.LogInformation("Rebuilding the collection (existing points: {Existing}).", existingCount);
                await _repository.RecreateCollectionAsync(cancellationToken);
            }

            await EmbedAndStoreAsync(chunks, cancellationToken);

            _logger.LogInformation("Ingestion finished: {Count} chunks stored.", chunks.Count);
            return new IngestionResult(chunks.Count, Skipped: false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EmbedAndStoreAsync(IReadOnlyList<TextChunk> chunks, CancellationToken cancellationToken)
    {
        for (var offset = 0; offset < chunks.Count; offset += _options.EmbeddingBatchSize)
        {
            var batch = chunks.Skip(offset).Take(_options.EmbeddingBatchSize).ToList();
            var inputs = batch.Select(BuildEmbeddingInput).ToList();

            var embeddings = await _embeddingGenerator.GenerateAsync(inputs, cancellationToken: cancellationToken);
            if (embeddings.Count != batch.Count)
            {
                throw new InvalidOperationException(
                    $"The embedding service returned {embeddings.Count} vectors for {batch.Count} inputs.");
            }

            var records = new List<DocumentChunk>(batch.Count);
            for (var i = 0; i < batch.Count; i++)
            {
                var vector = embeddings[i].Vector;
                EnsureExpectedDimensions(vector.Length);
                records.Add(ToRecord(batch[i], vector));
            }

            await _repository.UpsertAsync(records, cancellationToken);
            _logger.LogInformation("Embedded and stored chunks {From}-{To} of {Total}.",
                offset + 1, offset + batch.Count, chunks.Count);
        }
    }

    /// <summary>
    /// The text that gets embedded starts with the document name and page number.
    /// This small "contextual header" helps the embedding model place a short chunk in its context.
    /// The stored text stays clean, without the header.
    /// </summary>
    internal static string BuildEmbeddingInput(TextChunk chunk) =>
        $"{chunk.DocumentName} - page {chunk.PageNumber}\n{chunk.Text}";

    private static DocumentChunk ToRecord(TextChunk chunk, ReadOnlyMemory<float> vector) => new()
    {
        Id = DeterministicGuid.Create($"{chunk.DocumentName}|{chunk.PageNumber}|{chunk.ChunkIndex}"),
        DocumentName = chunk.DocumentName,
        PageNumber = chunk.PageNumber,
        ChunkIndex = chunk.ChunkIndex,
        Text = chunk.Text,
        Embedding = vector,
    };

    private static void EnsureExpectedDimensions(int actual)
    {
        if (actual != DocumentChunk.EmbeddingDimensions)
        {
            throw new InvalidOperationException(
                $"The embedding model returned {actual} dimensions but the collection expects " +
                $"{DocumentChunk.EmbeddingDimensions}. Update DocumentChunk.EmbeddingDimensions to match your embedding deployment.");
        }
    }

    private string ResolvePdfPath() =>
        Path.IsPathRooted(_options.PdfPath)
            ? _options.PdfPath
            : Path.Combine(_environment.ContentRootPath, _options.PdfPath);
}
