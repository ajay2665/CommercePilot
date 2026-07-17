using Commerce.Application.Abstractions;
using Commerce.Application.Features.Support;
using Commerce.Infrastructure.Workers;
using FastEndpoints;

namespace Commerce.Api.Features.Platform;

public sealed record StatusResponse(int Queued, Guid? ProcessingTicketId, string? ProcessingSubject, string? Stage);

/// <summary>Dashboard header pill: queue depth + what the worker is doing right now.</summary>
public sealed class StatusEndpoint : EndpointWithoutRequest<StatusResponse>
{
    private readonly IWorkQueue<TriageWorkItem> _queue;
    private readonly TriageStatus _status;

    public StatusEndpoint(IWorkQueue<TriageWorkItem> queue, TriageStatus status)
    {
        _queue = queue;
        _status = status;
    }

    public override void Configure()
    {
        Get("/api/status");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        Response = new StatusResponse(
            _queue.PendingCount, _status.CurrentTicketId, _status.CurrentSubject, _status.Stage);
        return Task.CompletedTask;
    }
}
