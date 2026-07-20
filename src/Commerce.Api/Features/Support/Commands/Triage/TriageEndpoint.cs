using Commerce.Application.Features.Support;
using FastEndpoints;

namespace Commerce.Api.Features.Support.Commands.Triage;

/// <summary>
/// Intake (local mode: dashboard submit form; Gmail poller posts here too once
/// toggled on). Returns 202 — the TriageWorker classifies asynchronously.
/// </summary>
public sealed class TriageEndpoint : Endpoint<TriageRequest, TriageResponse>
{
    public const string Route = "/api/support/triage";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous(); // Phase-1 static key is enforced by middleware; JWT roles in Phase 4
    }

    public override async Task HandleAsync(TriageRequest req, CancellationToken ct)
    {
        var result = await new SubmitTicketCommand(req.Subject, req.Body, req.Sender).ExecuteAsync(ct);
        await Send.ResponseAsync(new TriageResponse(result.TicketId, "queued"), 202, ct);
    }
}
