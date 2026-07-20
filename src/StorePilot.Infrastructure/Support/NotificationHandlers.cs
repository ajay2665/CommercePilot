using StorePilot.Application.Abstractions;
using StorePilot.Application.Events;
using StorePilot.Application.Options;
using StorePilot.Domain;
using Microsoft.Extensions.Options;

namespace StorePilot.Infrastructure.Support;

/// <summary>Routed-team notification on every triaged ticket (sequence §4.1).</summary>
public sealed class TicketCreatedNotificationHandler(ITicketRepository tickets, INotifier notifier)
    : IEventHandler<TicketCreated>
{
    public async Task HandleAsync(TicketCreated @event, CancellationToken ct)
    {
        var ticket = await tickets.GetAsync(@event.TicketId, ct);
        if (ticket is null) return;

        string subject = ticket.Subject.Length > 80 ? ticket.Subject[..80] + "…" : ticket.Subject;
        await notifier.NotifyAsync(new NotificationMessage(
            Channel: ticket.AssignedTeam ?? "General Support",
            Title: $"[{ticket.Brand}] {ticket.Category} · {ticket.Urgency} — {subject}",
            Message: ticket.Summary ?? subject,
            Severity: ticket.Urgency == TicketUrgency.High ? NotificationSeverity.Warning : NotificationSeverity.Info,
            TicketId: ticket.Id), ct);
    }
}

/// <summary>Escalation branch: high urgency + low confidence → escalation channel (Critical).</summary>
public sealed class TicketEscalatedNotificationHandler(
    ITicketRepository tickets,
    INotifier notifier,
    IOptions<SupportOptions> options) : IEventHandler<TicketEscalated>
{
    public async Task HandleAsync(TicketEscalated @event, CancellationToken ct)
    {
        var ticket = await tickets.GetAsync(@event.TicketId, ct);
        if (ticket is null) return;

        await notifier.NotifyAsync(new NotificationMessage(
            Channel: options.Value.EscalationChannel,
            Title: $"ESCALATION [{ticket.Brand}] {ticket.Subject}",
            Message: @event.Reason,
            Severity: NotificationSeverity.Critical,
            TicketId: ticket.Id), ct);
    }
}
