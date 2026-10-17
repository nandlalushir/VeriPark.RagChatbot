using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using VeriPark.RagChatbot.Api.Chat;
using VeriPark.RagChatbot.Api.Configuration;
using VeriPark.RagChatbot.Api.Contracts;
using VeriPark.RagChatbot.Api.Ingestion;

namespace VeriPark.RagChatbot.Api.Endpoints;

public static partial class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", (IngestionState state) => Results.Ok(state.Snapshot))
            .WithName("Health");

        var api = app.MapGroup("/api");

        api.MapPost("/chat", AskAsync).WithName("Chat");
        api.MapGet("/conversations/{conversationId}", GetConversation).WithName("GetConversation");
        api.MapDelete("/conversations/{conversationId}", DeleteConversation).WithName("DeleteConversation");
        api.MapPost("/ingest", IngestAsync).WithName("Ingest");

        return app;
    }

    private static async Task<IResult> AskAsync(
        ChatRequest request,
        IRagChatService chatService,
        IngestionState ingestionState,
        IOptions<RagOptions> options,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request, options.Value.MaxQuestionLength);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        if (!ingestionState.IsReady)
        {
            var snapshot = ingestionState.Snapshot;
            return Results.Problem(
                title: "The document knowledge base is not ready yet.",
                detail: $"Status: {snapshot.Status}. {snapshot.Error}".Trim(),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var logger = loggerFactory.CreateLogger(typeof(ChatEndpoints).FullName!);

        try
        {
            var response = await chatService.AskAsync(request.ConversationId, request.Message.Trim(), cancellationToken);
            return Results.Ok(response);
        }
        catch (HttpOperationException ex)
        {
            logger.LogError(ex, "Azure OpenAI call failed with status {Status}.", ex.StatusCode);
            return Results.Problem(
                title: "The Azure OpenAI request failed.",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static IResult GetConversation(string conversationId, IConversationStore store)
    {
        var session = store.Find(conversationId);
        return session is null
            ? Results.NotFound()
            : Results.Ok(new ConversationHistoryResponse(session.Id, session.GetTurns()));
    }

    private static IResult DeleteConversation(string conversationId, IConversationStore store) =>
        store.Remove(conversationId) ? Results.NoContent() : Results.NotFound();

    private static async Task<IResult> IngestAsync(
        IDocumentIngestionService ingestionService,
        IngestionState state,
        CancellationToken cancellationToken,
        bool force = false)
    {
        try
        {
            state.MarkRunning();
            var result = await ingestionService.IngestAsync(force, cancellationToken);
            state.MarkReady(result.TotalChunks);

            var message = result.Skipped
                ? "The collection was already up to date."
                : "The document was embedded and stored.";
            return Results.Ok(new IngestResponse(result.TotalChunks, result.Skipped, message));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            state.MarkFailed(ex.Message);
            throw;
        }
    }

    private static Dictionary<string, string[]> Validate(ChatRequest request, int maxLength)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            errors["message"] = ["Message is required."];
        }
        else if (request.Message.Length > maxLength)
        {
            errors["message"] = [$"Message must be at most {maxLength} characters."];
        }

        if (!string.IsNullOrEmpty(request.ConversationId) && !ConversationIdRegex().IsMatch(request.ConversationId))
        {
            errors["conversationId"] = ["Conversation id may only contain letters, digits, '-' and '_' (max 64 characters)."];
        }

        return errors;
    }

    [GeneratedRegex(@"^[A-Za-z0-9_-]{1,64}$")]
    private static partial Regex ConversationIdRegex();
}
