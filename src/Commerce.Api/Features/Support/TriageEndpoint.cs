using Commerce.Application.Features.Support;
using FastEndpoints;
using FluentValidation;

namespace Commerce.Api.Features.Support;

public sealed class TriageRequest
{
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public string Sender { get; set; } = "";
}

public sealed record TriageResponse(Guid TicketId, string Status);

public sealed class TriageValidator : Validator<TriageRequest>
{
    public TriageValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Body).NotEmpty();
        RuleFor(x => x.Sender).NotEmpty().MaximumLength(256);
    }
}

/// <summary>
/// Intake (local mode: dashboard submit form; Gmail poller posts here too once
/// toggled on). Returns 202 — the TriageWorker classifies asynchronously.
/// </summary>
public sealed class TriageEndpoint : Endpoint<TriageRequest, TriageResponse>
{
    public override void Configure()
    {
        Post("/api/support/triage");
        AllowAnonymous(); // Phase-1 static key is enforced by middleware; JWT roles in Phase 4
    }

    public override async Task HandleAsync(TriageRequest req, CancellationToken ct)
    {
        var result = await new SubmitTicketCommand(req.Subject, req.Body, req.Sender).ExecuteAsync(ct);
        await Send.ResponseAsync(new TriageResponse(result.TicketId, "queued"), 202, ct);
    }
}
