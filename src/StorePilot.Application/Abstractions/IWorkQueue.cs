namespace StorePilot.Application.Abstractions;

/// <summary>Unbounded in-memory work queue feeding a background worker (SupportDeskAI's TicketQueue pattern).</summary>
public interface IWorkQueue<T>
{
    int PendingCount { get; }
    void Enqueue(T item);
    IAsyncEnumerable<T> DequeueAllAsync(CancellationToken ct);
}
