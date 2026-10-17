namespace VeriPark.RagChatbot.Api.Domain;

/// <summary>
/// The in-memory state of one chat conversation.
/// The <see cref="Gate"/> makes sure two requests for the same conversation never run at the same time,
/// so the history always stays in the right order.
/// </summary>
public sealed class ConversationSession
{
    private readonly List<ConversationTurn> _turns = [];
    private readonly object _sync = new();
    private long _lastAccessTicks;

    public ConversationSession(string id, DateTimeOffset createdAtUtc)
    {
        Id = id;
        CreatedAtUtc = createdAtUtc;
        _lastAccessTicks = createdAtUtc.UtcTicks;
    }

    public string Id { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public SemaphoreSlim Gate { get; } = new(1, 1);

    public DateTimeOffset LastAccessUtc => new(Interlocked.Read(ref _lastAccessTicks), TimeSpan.Zero);

    public void Touch(DateTimeOffset nowUtc) => Interlocked.Exchange(ref _lastAccessTicks, nowUtc.UtcTicks);

    public IReadOnlyList<ConversationTurn> GetTurns()
    {
        lock (_sync)
        {
            return _turns.ToArray();
        }
    }

    public void AddTurn(ConversationTurn turn)
    {
        lock (_sync)
        {
            _turns.Add(turn);
        }
    }
}
