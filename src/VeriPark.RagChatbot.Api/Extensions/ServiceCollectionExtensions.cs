using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using CommunityToolkit.VectorData.Qdrant;
using Qdrant.Client;
using VeriPark.RagChatbot.Api.Chat;
using VeriPark.RagChatbot.Api.Configuration;
using VeriPark.RagChatbot.Api.Ingestion;
using VeriPark.RagChatbot.Api.Retrieval;

namespace VeriPark.RagChatbot.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Binds and validates configuration. The application refuses to start if a required value is missing.</summary>
    public static IServiceCollection AddRagOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AzureOpenAIOptions>()
            .Bind(configuration.GetSection(AzureOpenAIOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<QdrantOptions>()
            .Bind(configuration.GetSection(QdrantOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RagOptions>()
            .Bind(configuration.GetSection(RagOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Registers the Azure OpenAI chat completion service and the embedding generator through Semantic Kernel.
    /// Both are resolved from dependency injection by the rest of the application.
    /// </summary>
    public static IServiceCollection AddAzureOpenAIServices(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AzureOpenAIOptions.SectionName).Get<AzureOpenAIOptions>()
            ?? new AzureOpenAIOptions();

        // The kernel builder needs the values immediately, so validate them here to give a clear startup error.
        try
        {
            Validator.ValidateObject(options, new ValidationContext(options), validateAllProperties: true);
        }
        catch (ValidationException ex)
        {
            throw new InvalidOperationException(
                $"Azure OpenAI configuration is incomplete: {ex.Message} " +
                "Set the values with 'dotnet user-secrets' or environment variables (see README).", ex);
        }

#pragma warning disable SKEXP0010 // Semantic Kernel experimental API
        services.AddKernel()
            .AddAzureOpenAIChatCompletion(
                deploymentName: options.ChatDeployment,
                endpoint: options.Endpoint,
                apiKey: options.ApiKey)
            .AddAzureOpenAIEmbeddingGenerator(
                deploymentName: options.EmbeddingDeployment,
                endpoint: options.Endpoint,
                apiKey: options.ApiKey);
#pragma warning restore SKEXP0010

        return services;
    }

    public static IServiceCollection AddVectorStore(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var qdrant = provider.GetRequiredService<IOptions<QdrantOptions>>().Value;
            var apiKey = string.IsNullOrWhiteSpace(qdrant.ApiKey) ? null : qdrant.ApiKey;
            return new QdrantClient(qdrant.Host, qdrant.GrpcPort, qdrant.UseHttps, apiKey);
        });

        services.AddSingleton(provider =>
            new QdrantVectorStore(provider.GetRequiredService<QdrantClient>(), ownsClient: false));

        services.AddSingleton<IDocumentChunkRepository, QdrantDocumentChunkRepository>();
        return services;
    }

    public static IServiceCollection AddRagPipeline(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        // Ingestion
        services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
        services.AddSingleton<IDocumentChunker, RecursiveTextChunker>();
        services.AddSingleton<IngestionState>();
        services.AddSingleton<IDocumentIngestionService, DocumentIngestionService>();
        services.AddHostedService<IngestionHostedService>();

        // Retrieval + chat
        services.AddSingleton<IDocumentRetriever, DocumentRetriever>();
        services.AddSingleton<IConversationStore, InMemoryConversationStore>();
        services.AddSingleton<IQueryRewriter, QueryRewriter>();
        services.AddSingleton<IRagChatService, RagChatService>();

        return services;
    }
}
