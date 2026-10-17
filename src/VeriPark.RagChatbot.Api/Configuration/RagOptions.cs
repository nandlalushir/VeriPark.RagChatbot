using System.ComponentModel.DataAnnotations;

namespace VeriPark.RagChatbot.Api.Configuration;

/// <summary>Tunable settings for ingestion, retrieval and the chat flow.</summary>
public sealed class RagOptions : IValidatableObject
{
    public const string SectionName = "Rag";

    [Required]
    public string DocumentTitle { get; set; } = "Microsoft Copilot Studio Licensing Guide";

    /// <summary>Path to the PDF. Relative paths are resolved from the application content root.</summary>
    [Required]
    public string PdfPath { get; set; } = "data/Microsoft-Copilot-Studio-Licensing-Guide.pdf";

    /// <summary>Maximum characters per chunk (roughly 300 tokens at the default).</summary>
    [Range(200, 4000)]
    public int ChunkSize { get; set; } = 1200;

    /// <summary>Characters shared between neighbouring chunks of the same page.</summary>
    [Range(0, 1000)]
    public int ChunkOverlap { get; set; } = 200;

    [Range(1, 128)]
    public int EmbeddingBatchSize { get; set; } = 16;

    /// <summary>How many chunks are fetched from Qdrant for every question.</summary>
    [Range(1, 20)]
    public int TopK { get; set; } = 4;

    /// <summary>Chunks scoring below this cosine similarity are dropped.</summary>
    [Range(0.0, 1.0)]
    public double MinRelevanceScore { get; set; } = 0.2;

    /// <summary>How many previous question/answer pairs are sent to the model.</summary>
    [Range(1, 100)]
    public int MaxHistoryTurns { get; set; } = 20;

    [Range(1, 1440)]
    public int ConversationTimeoutMinutes { get; set; } = 120;

    [Range(1, 10000)]
    public int MaxQuestionLength { get; set; } = 2000;

    /// <summary>
    /// Sampling temperature. Leave it empty (null) if your chat deployment is a reasoning model
    /// that does not accept a custom temperature.
    /// </summary>
    [Range(0.0, 2.0)]
    public double? ChatTemperature { get; set; } = 0.1;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChunkOverlap >= ChunkSize)
        {
            yield return new ValidationResult(
                "Rag:ChunkOverlap must be smaller than Rag:ChunkSize.",
                [nameof(ChunkOverlap), nameof(ChunkSize)]);
        }
    }
}
