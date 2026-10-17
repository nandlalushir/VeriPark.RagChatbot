namespace VeriPark.RagChatbot.Api.Ingestion;

public sealed record IngestionSnapshot(string Status, bool IsReady, int ChunkCount, string? Error);

/// <summary>
/// Thread-safe flag that tells the chat endpoint (and /health) whether the PDF has been loaded into Qdrant.
/// Ingestion runs in the background so the web server can start even when Qdrant is still booting.
/// </summary>
public sealed class IngestionState
{
    private volatile IngestionSnapshot _snapshot = new("NotStarted", false, 0, null);

    public IngestionSnapshot Snapshot => _snapshot;

    public bool IsReady => _snapshot.IsReady;

    public void MarkRunning() => _snapshot = new IngestionSnapshot("Running", false, _snapshot.ChunkCount, null);

    public void MarkReady(int chunkCount) => _snapshot = new IngestionSnapshot("Ready", true, chunkCount, null);

    public void MarkFailed(string error) => _snapshot = new IngestionSnapshot("Failed", false, _snapshot.ChunkCount, error);
}
