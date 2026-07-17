using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Channels;
using Commerce.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Messaging;

/// <summary>
/// Decision 3: in-process Channel fan-out behind IEventBus. A RabbitMQ
/// implementation replaces this class in Phase 4 — publishers and handlers
/// never change.
/// </summary>
public sealed class InProcessEventBus : IEventBus
{
    private readonly Channel<object> _channel = Channel.CreateUnbounded<object>();

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : notnull
        => _channel.Writer.WriteAsync(@event, ct).AsTask();

    internal IAsyncEnumerable<object> ReadAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}

/// <summary>
/// Drains the bus and dispatches each event to every registered
/// IEventHandler&lt;TEvent&gt; in its own DI scope. Handler failures are logged,
/// never rethrown — one bad handler must not stall the bus.
/// </summary>
public sealed class EventDispatcherService(
    InProcessEventBus bus,
    IServiceScopeFactory scopeFactory,
    ILogger<EventDispatcherService> logger) : BackgroundService
{
    private static readonly ConcurrentDictionary<Type, (Type HandlerType, MethodInfo Method)> _cache = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (object @event in bus.ReadAllAsync(stoppingToken))
        {
            var (handlerType, method) = _cache.GetOrAdd(@event.GetType(), static eventType =>
            {
                Type ht = typeof(IEventHandler<>).MakeGenericType(eventType);
                return (ht, ht.GetMethod(nameof(IEventHandler<object>.HandleAsync))!);
            });

            using IServiceScope scope = scopeFactory.CreateScope();
            foreach (object? handler in scope.ServiceProvider.GetServices(handlerType))
            {
                if (handler is null) continue;
                try
                {
                    await (Task)method.Invoke(handler, [@event, stoppingToken])!;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Event handler {Handler} failed for {Event}",
                        handler.GetType().Name, @event.GetType().Name);
                }
            }
        }
    }
}
