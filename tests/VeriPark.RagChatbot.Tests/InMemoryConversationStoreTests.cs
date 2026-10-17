using Microsoft.Extensions.Options;
using VeriPark.RagChatbot.Api.Chat;
using VeriPark.RagChatbot.Api.Configuration;
using VeriPark.RagChatbot.Api.Domain;
using Xunit;

namespace VeriPark.RagChatbot.Tests;

public class InMemoryConversationStoreTests
{
    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan amount) => _now += amount;
    }

    private static (InMemoryConversationStore Store, ManualTimeProvider Time) Create(int timeoutMinutes = 30)
    {
        var time = new ManualTimeProvider();
        var store = new InMemoryConversationStore(time, Options.Create(new RagOptions { ConversationTimeoutMinutes = timeoutMinutes }));
        return (store, time);
    }

    [Fact]
    public void GetOrCreate_WithoutId_CreatesNewConversationEachTime()
    {
        var (store, _) = Create();

        var first = store.GetOrCreate(null);
        var second = store.GetOrCreate(string.Empty);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void GetOrCreate_WithSameId_ReturnsSameSessionAndKeepsHistory()
    {
        var (store, time) = Create();
        var session = store.GetOrCreate("abc");
        session.AddTurn(new ConversationTurn("Q1", "A1", [], time.GetUtcNow()));

        var again = store.GetOrCreate("abc");

        Assert.Same(session, again);
        Assert.Single(again.GetTurns());
    }

    [Fact]
    public void IdleConversation_IsRemovedAfterTimeout()
    {
        var (store, time) = Create(timeoutMinutes: 30);
        store.GetOrCreate("abc");

        time.Advance(TimeSpan.FromMinutes(31));

        Assert.Null(store.Find("abc"));
    }

    [Fact]
    public void ActiveConversation_SurvivesBecauseAccessRefreshesTheTimer()
    {
        var (store, time) = Create(timeoutMinutes: 30);
        store.GetOrCreate("abc");

        time.Advance(TimeSpan.FromMinutes(20));
        store.GetOrCreate("abc");
        time.Advance(TimeSpan.FromMinutes(20));

        Assert.NotNull(store.Find("abc"));
    }

    [Fact]
    public void Remove_DeletesTheConversation()
    {
        var (store, _) = Create();
        store.GetOrCreate("abc");

        Assert.True(store.Remove("abc"));
        Assert.Null(store.Find("abc"));
        Assert.False(store.Remove("abc"));
    }
}
