using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using VeriPark.RagChatbot.Api.Configuration;
using VeriPark.RagChatbot.Api.Contracts;
using VeriPark.RagChatbot.Api.Domain;
using VeriPark.RagChatbot.Api.Retrieval;

namespace VeriPark.RagChatbot.Api.Chat;

/// <summary>
/// Orchestrates one chat turn:
/// <list type="number">
/// <item>load the conversation history,</item>
/// <item>rewrite the question so follow-ups are understandable on their own,</item>
/// <item>retrieve the best matching chunks from Qdrant,</item>
/// <item>build a grounded prompt (system rules + history + context + question),</item>
/// <item>call the Azure OpenAI chat model through Semantic Kernel,</item>
/// <item>store the turn and return the answer with citations.</item>
/// </list>
/// </summary>
public sealed class RagChatService : IRagChatService
{
    private readonly IChatCompletionService _chatCompletion;
    private readonly IQueryRewriter _queryRewriter;
    private readonly IDocumentRetriever _retriever;
    private readonly IConversationStore _conversationStore;
    private readonly TimeProvider _timeProvider;
    private readonly RagOptions _options;
    private readonly ILogger<RagChatService> _logger;

    public RagChatService(
        IChatCompletionService chatCompletion,
        IQueryRewriter queryRewriter,
        IDocumentRetriever retriever,
        IConversationStore conversationStore,
        TimeProvider timeProvider,
        IOptions<RagOptions> options,
        ILogger<RagChatService> logger)
    {
        _chatCompletion = chatCompletion;
        _queryRewriter = queryRewriter;
        _retriever = retriever;
        _conversationStore = conversationStore;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ChatResponse> AskAsync(string? conversationId, string message, CancellationToken cancellationToken)
    {
        var session = _conversationStore.GetOrCreate(conversationId);

        // One request at a time per conversation keeps the history in the right order.
        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            var turns = session.GetTurns();

            var standaloneQuestion = await _queryRewriter.RewriteAsync(message, turns, cancellationToken);
            var chunks = await _retriever.RetrieveAsync(standaloneQuestion, cancellationToken);

            var chatHistory = BuildChatHistory(turns, message, standaloneQuestion, chunks);
            var reply = await _chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                ChatExecutionSettings.Create(_options.ChatTemperature),
                kernel: null,
                cancellationToken: cancellationToken);

            var answer = reply.Content?.Trim() ?? string.Empty;
            var citations = CitationParser.Build(answer, chunks);

            session.AddTurn(new ConversationTurn(message, answer, citations, _timeProvider.GetUtcNow()));

            _logger.LogInformation(
                "Conversation {ConversationId}: turn {Turn} answered with {Chunks} chunks and {Citations} citations.",
                session.Id, turns.Count + 1, chunks.Count, citations.Count);

            return new ChatResponse(session.Id, answer, citations, standaloneQuestion);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private ChatHistory BuildChatHistory(
        IReadOnlyList<ConversationTurn> turns,
        string message,
        string standaloneQuestion,
        IReadOnlyList<RetrievedChunk> chunks)
    {
        var chatHistory = new ChatHistory(PromptTemplates.AnswerSystem);

        // Earlier turns are replayed as plain user/assistant messages. The retrieved context of old turns is
        // NOT replayed, which keeps the prompt small; each new question gets its own fresh context.
        foreach (var turn in turns.TakeLast(_options.MaxHistoryTurns))
        {
            chatHistory.AddUserMessage(turn.Question);
            chatHistory.AddAssistantMessage(turn.Answer);
        }

        chatHistory.AddUserMessage(BuildUserPrompt(message, standaloneQuestion, chunks));
        return chatHistory;
    }

    internal static string BuildUserPrompt(
        string message,
        string standaloneQuestion,
        IReadOnlyList<RetrievedChunk> chunks)
    {
        var builder = new StringBuilder();
        builder.AppendLine("CONTEXT:");

        if (chunks.Count == 0)
        {
            builder.AppendLine(PromptTemplates.NoContextMarker);
        }
        else
        {
            for (var i = 0; i < chunks.Count; i++)
            {
                builder.AppendLine($"[{i + 1}] (page {chunks[i].PageNumber})");
                builder.AppendLine(chunks[i].Text);
                builder.AppendLine();
            }
        }

        builder.AppendLine("---");
        builder.Append("QUESTION: ").AppendLine(message);

        if (!string.Equals(message, standaloneQuestion, StringComparison.Ordinal))
        {
            builder.Append("(The question above, written as a standalone question: ").Append(standaloneQuestion).AppendLine(")");
        }

        return builder.ToString();
    }
}
