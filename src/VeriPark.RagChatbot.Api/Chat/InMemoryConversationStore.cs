using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using VeriPark.RagChatbot.Api.Configuration;
using VeriPark.RagChatbot.Api.Domain;

namespace VeriPark.RagChatbot.Api.Chat;

/// <summary>
/// Thread-safe, in-memory conversation storage. Conversations that were idle for longer than
/// <c>Rag:ConversationTimeoutMinutes</c> are removed so memory cannot grow forever.
/// The history is lost when the application restarts, which is what the assignment asks for ("in memory").
/// </summary>
public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, ConversationSession> _sessions = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _idleTimeout;

    public InMemoryConversationStore(TimeProvider timeProvider, IOptions<RagOptions> options)
    {
        _timeProvider = timeProvider;
        _idleTimeout = TimeSpan.FromMinutes(options.Value.ConversationTimeoutMinutes);
    }

    public ConversationSession GetOrCreate(string? conversationId)
    {
        RemoveExpiredSessions();

        var id = string.IsNullOrWhiteSpace(conversationId) ? Guid.NewGuid().ToString("N") : conversationId;
        var now = _timeProvider.GetUtcNow();

        var session = _sessions.GetOrAdd(id, key => new ConversationSession(key, now));
        session.Touch(now);
        return session;
    }

    public ConversationSession? Find(string conversationId)
    {
        RemoveExpiredSessions();

        if (!_sessions.TryGetValue(conversationId, out var session))
        {
            return null;
        }

        session.Touch(_timeProvider.GetUtcNow());
        return session;
    }

    public bool Remove(string conversationId) => _sessions.TryRemove(conversationId, out _);

    private void RemoveExpiredSessions()
    {
        var now = _timeProvider.GetUtcNow();
        foreach (var pair in _sessions)
        {
            if (now - pair.Value.LastAccessUtc > _idleTimeout)
            {
                _sessions.TryRemove(pair);
            }
        }
    }
}
