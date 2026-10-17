using System.ComponentModel.DataAnnotations;

namespace VeriPark.RagChatbot.Api.Configuration;

/// <summary>Connection settings for the user's own Azure OpenAI resource.</summary>
public sealed class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    [Required(AllowEmptyStrings = false, ErrorMessage = "AzureOpenAI:Endpoint is required (for example https://my-resource.openai.azure.com/).")]
    [Url]
    public string Endpoint { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "AzureOpenAI:ApiKey is required. Set it with user-secrets or an environment variable.")]
    public string ApiKey { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "AzureOpenAI:ChatDeployment is required (the deployment name of your chat model).")]
    public string ChatDeployment { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "AzureOpenAI:EmbeddingDeployment is required (the deployment name of your embedding model).")]
    public string EmbeddingDeployment { get; set; } = string.Empty;
}
