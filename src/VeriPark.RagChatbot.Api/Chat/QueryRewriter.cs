using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using VeriPark.RagChatbot.Api.Configuration;
using VeriPark.RagChatbot.Api.Domain;

namespace VeriPark.RagChatbot.Api.Chat;

/// <summary>
/// A follow-up such as "compare that with the credit pack" has no meaning for a vector search on its own.
/// This step asks the chat model to rewrite it using the recent conversation, for example
/// "Compare the pay-as-you-go Copilot Credit meter with the Copilot Credit pack subscription".
/// </summary>
public sealed class QueryRewriter : IQueryRewriter
{
    private const int RecentTurnsUsed = 4;
    private const int MaxAnswerCharactersInPrompt = 700;

    private readonly IChatCompletionService _chatCompletion;
    private readonly RagOptions _options;
    private readonly ILogger<QueryRewriter> _logger;

    public QueryRewriter(
        IChatCompletionService chatCompletion,
        IOptions<RagOptions> options,
        ILogger<QueryRewriter> logger)
    {
        _chatCompletion = chatCompletion;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> RewriteAsync(
        string question,
        IReadOnlyList<ConversationTurn> history,
        CancellationToken cancellationToken)
    {
        // First message of a conversation: nothing to resolve, save one model call.
        if (history.Count == 0)
        {
            return question;
        }

        var chat = new ChatHistory(PromptTemplates.QueryRewriteSystem);
        chat.AddUserMessage(BuildInput(question, history));

        var response = await _chatCompletion.GetChatMessageContentAsync(
            chat,
            ChatExecutionSettings.Create(_options.ChatTemperature),
            kernel: null,
            cancellationToken: cancellationToken);

        var rewritten = response.Content?.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(rewritten))
        {
            return question;
        }

        _logger.LogDebug("Rewrote follow-up '{Original}' as '{Rewritten}'.", question, rewritten);
        return rewritten;
    }

    private static string BuildInput(string question, IReadOnlyList<ConversationTurn> history)
    {
        var builder = new StringBuilder("Conversation so far:\n");

        foreach (var turn in history.TakeLast(RecentTurnsUsed))
        {
            builder.Append("User: ").AppendLine(turn.Question);
            builder.Append("Assistant: ").AppendLine(Truncate(turn.Answer, MaxAnswerCharactersInPrompt));
        }

        builder.AppendLine().Append("Latest user message: ").Append(question);
        return builder.ToString();
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "...";
}
