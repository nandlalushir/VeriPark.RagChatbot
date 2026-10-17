using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace VeriPark.RagChatbot.Api.Chat;

internal static class ChatExecutionSettings
{
    /// <summary>
    /// A null temperature means "do not send the parameter", which is required for reasoning models
    /// that only accept their default temperature.
    /// </summary>
    public static AzureOpenAIPromptExecutionSettings Create(double? temperature) => new()
    {
        Temperature = temperature,
    };
}
