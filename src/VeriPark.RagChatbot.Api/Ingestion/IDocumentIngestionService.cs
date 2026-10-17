namespace VeriPark.RagChatbot.Api.Ingestion;

public sealed record IngestionResult(int TotalChunks, bool Skipped);

/// <summary>Loads the PDF, chunks it, embeds the chunks and stores them in the vector database.</summary>
public interface IDocumentIngestionService
{
    /// <param name="force">When true the collection is deleted and rebuilt even if it looks up to date.</param>
    Task<IngestionResult> IngestAsync(bool force, CancellationToken cancellationToken);
}
