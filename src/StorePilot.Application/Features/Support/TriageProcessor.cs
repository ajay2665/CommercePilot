using StorePilot.Application.Abstractions;
using StorePilot.Application.Events;
using StorePilot.Application.Options;
using StorePilot.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StorePilot.Application.Features.Support;

/// <summary>
/// The per-ticket triage pipeline the worker drives: classify → route → persist
/// → publish events. Grows into the full draft/QA/approval workflow in Phase 1b.
/// </summary>
public sealed class TriageProcessor(
    ITicketRepository tickets,
    IRoutingRuleRepository routingRules,
    ITriageClassifier classifier,
    IEventBus bus,
    IOptions<SupportOptions> options,
    ILogger<TriageProcessor> logger)
{
    public async Task ProcessAsync(Guid ticketId, CancellationToken ct)
    {
        var ticket = await tickets.GetAsync(ticketId, ct);
        if (ticket is null)
        {
            logger.LogWarning("Triage skipped: ticket {TicketId} not found", ticketId);
            return;
        }
        if (ticket.Status != TicketStatus.Queued)
        {
            logger.LogWarning("Triage skipped: ticket {TicketId} already {Status}", ticketId, ticket.Status);
            return;
        }

        TicketClassification c = await classifier.ClassifyAsync(
            new TriageInput(ticket.Subject, ticket.Body, ticket.Sender), ct);

        ticket.Brand = c.Brand;
        ticket.Category = c.Category;
        ticket.Urgency = c.Urgency;
        ticket.Sentiment = c.Sentiment;
        ticket.Confidence = c.Confidence;
        ticket.Summary = c.Summary;
        ticket.TriagedAt = DateTimeOffset.UtcNow;

        var rules = await routingRules.GetAllAsync(ct);
        var rule = RoutingResolver.Resolve(rules, c.Brand, c.Category);
        ticket.AssignedTeam = rule?.TargetTeam ?? options.Value.DefaultTeam;

        bool escalate = c.Urgency == TicketUrgency.High
                     && c.Confidence < options.Value.EscalationConfidenceThreshold;
        ticket.Status = escalate ? TicketStatus.Escalated : TicketStatus.Triaged;

        await tickets.SaveChangesAsync(ct);
        logger.LogInformation(
            "Ticket {TicketId} triaged: {Brand} / {Category} / {Urgency} (confidence {Confidence:F2}) → {Team}{Escalated}",
            ticket.Id, c.Brand, c.Category, c.Urgency, c.Confidence, ticket.AssignedTeam,
            escalate ? " [ESCALATED]" : "");

        await bus.PublishAsync(new TicketCreated(ticket.Id), ct);
        if (escalate)
            await bus.PublishAsync(new TicketEscalated(
                ticket.Id,
                $"High urgency with confidence {c.Confidence:F2} below threshold {options.Value.EscalationConfidenceThreshold:F2}"), ct);
    }
}
