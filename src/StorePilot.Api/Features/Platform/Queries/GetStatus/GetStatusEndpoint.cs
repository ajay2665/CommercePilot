using StorePilot.Application.Abstractions;
using StorePilot.Application.Features.Support;
using StorePilot.Infrastructure.Workers;
using FastEndpoints;

namespace StorePilot.Api.Features.Platform.Queries.GetStatus;

/// <summary>Dashboard header pill: queue depth + what the worker is doing right now.</summary>
public sealed class GetStatusEndpoint : EndpointWithoutRequest<StatusResponse>
{
    public const string Route = "/api/status";

    private readonly IWorkQueue<TriageWorkItem> _queue;
    private readonly TriageStatus _status;

    public GetStatusEndpoint(IWorkQueue<TriageWorkItem> queue, TriageStatus status)
    {
        _queue = queue;
        _status = status;
    }

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        Response = new StatusResponse(
            _queue.PendingCount, _status.CurrentTicketId, _status.CurrentSubject, _status.Stage);
        return Task.CompletedTask;
    }
}
