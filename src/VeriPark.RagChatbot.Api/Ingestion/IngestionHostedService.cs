namespace VeriPark.RagChatbot.Api.Ingestion;

/// <summary>
/// Loads the PDF into Qdrant in the background when the application starts.
/// It retries a few times because the Qdrant container may need a few seconds to be ready.
/// </summary>
public sealed class IngestionHostedService : BackgroundService
{
    private const int MaxAttempts = 5;

    private readonly IDocumentIngestionService _ingestionService;
    private readonly IngestionState _state;
    private readonly ILogger<IngestionHostedService> _logger;

    public IngestionHostedService(
        IDocumentIngestionService ingestionService,
        IngestionState state,
        ILogger<IngestionHostedService> logger)
    {
        _ingestionService = ingestionService;
        _state = state;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    _state.MarkRunning();
                    var result = await _ingestionService.IngestAsync(force: false, stoppingToken);
                    _state.MarkReady(result.TotalChunks);
                    return;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _state.MarkFailed(ex.Message);
                    _logger.LogWarning(ex, "Ingestion attempt {Attempt}/{Max} failed.", attempt, MaxAttempts);

                    if (attempt < MaxAttempts)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5 * attempt), stoppingToken);
                    }
                }
            }

            _logger.LogError(
                "Ingestion failed after {Max} attempts. Fix the problem (is Qdrant running? are the Azure OpenAI settings correct?) " +
                "and call POST /api/ingest to try again.", MaxAttempts);
        }
        catch (OperationCanceledException)
        {
            // Application is shutting down.
        }
    }
}
