using StorePilot.Application.Abstractions;
using System.Threading.Channels;

namespace StorePilot.Infrastructure.Messaging;

/// <summary>SupportDeskAI's TicketQueue, generalised: unbounded channel, one consumer worker.</summary>
public sealed class ChannelWorkQueue<T> : IWorkQueue<T>
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

    public int PendingCount => _channel.Reader.Count;

    public void Enqueue(T item) => _channel.Writer.TryWrite(item); // unbounded — always succeeds

    public IAsyncEnumerable<T> DequeueAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}
