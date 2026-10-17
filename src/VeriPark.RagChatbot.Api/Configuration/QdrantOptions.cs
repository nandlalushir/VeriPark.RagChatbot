using System.ComponentModel.DataAnnotations;

namespace VeriPark.RagChatbot.Api.Configuration;

/// <summary>Settings for the local Qdrant vector database.</summary>
public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";

    [Required]
    public string Host { get; set; } = "localhost";

    /// <summary>gRPC port. The .NET client talks gRPC (6334), not the REST port (6333).</summary>
    [Range(1, 65535)]
    public int GrpcPort { get; set; } = 6334;

    public bool UseHttps { get; set; }

    /// <summary>Optional. Only needed if your Qdrant instance is protected by an API key.</summary>
    public string? ApiKey { get; set; }

    [Required]
    public string CollectionName { get; set; } = "copilot_studio_licensing_guide";
}
