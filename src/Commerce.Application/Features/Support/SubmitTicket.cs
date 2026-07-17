using Commerce.Application.Abstractions;
using Commerce.Domain.Entities;
using FastEndpoints;

namespace Commerce.Application.Features.Support;

/// <summary>Work item flowing from intake (form / poller) to the TriageWorker.</summary>
public sealed record TriageWorkItem(Guid TicketId);

public sealed record SubmitTicketCommand(string Subject, string Body, string Sender)
    : ICommand<SubmitTicketResult>;

public sealed record SubmitTicketResult(Guid TicketId);

/// <summary>
/// Intake: persist the ticket immediately (visible in the dashboard as Queued),
/// then hand it to the background worker — the SupportDeskAI enqueue pattern.
/// </summary>
public sealed class SubmitTicketHandler(ITicketRepository tickets, IWorkQueue<TriageWorkItem> queue)
    : ICommandHandler<SubmitTicketCommand, SubmitTicketResult>
{
    public async Task<SubmitTicketResult> ExecuteAsync(SubmitTicketCommand command, CancellationToken ct)
    {
        var ticket = new Ticket
        {
            Subject = command.Subject.Trim(),
            Body = command.Body.Trim(),
            Sender = command.Sender.Trim(),
        };

        await tickets.AddAsync(ticket, ct);
        await tickets.SaveChangesAsync(ct);
        queue.Enqueue(new TriageWorkItem(ticket.Id));

        return new SubmitTicketResult(ticket.Id);
    }
}
