using System.Collections.Concurrent;

namespace StorePilot.Infrastructure.Orchestrator;

public sealed record ChatTurn(string Role, string Content);

/// <summary>
/// In-memory multi-turn chat store: sliding 30-minute TTL per conversation,
/// last 10 turns kept. Swap for a Redis-backed store when Phase 4 deploys Redis.
/// </summary>
public sealed class ConversationMemory
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);
    private const int MaxTurns = 10;

    private sealed class Entry
    {
        public List<ChatTurn> Turns { get; } = [];
        public DateTimeOffset LastActivity { get; set; } = DateTimeOffset.UtcNow;
    }

    private readonly ConcurrentDictionary<Guid, Entry> _conversations = new();

    public IReadOnlyList<ChatTurn> GetTurns(Guid conversationId)
    {
        Sweep();
        if (!_conversations.TryGetValue(conversationId, out var entry))
            return [];
        lock (entry.Turns)
            return entry.Turns.ToList();
    }

    public void Append(Guid conversationId, string userMessage, string assistantReply)
    {
        var entry = _conversations.GetOrAdd(conversationId, _ => new Entry());
        lock (entry.Turns)
        {
            entry.Turns.Add(new ChatTurn("user", userMessage));
            entry.Turns.Add(new ChatTurn("assistant", assistantReply));
            if (entry.Turns.Count > MaxTurns)
                entry.Turns.RemoveRange(0, entry.Turns.Count - MaxTurns);
            entry.LastActivity = DateTimeOffset.UtcNow;
        }
    }

    private void Sweep()
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - Ttl;
        foreach (var (id, entry) in _conversations)
            if (entry.LastActivity < cutoff)
                _conversations.TryRemove(id, out _);
    }
}
