namespace StorePilot.Application.Abstractions;

/// <summary>
/// In-process today (Channel fan-out), RabbitMQ later — feature code never changes.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : notnull;
}

public interface IEventHandler<in TEvent> where TEvent : notnull
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}
