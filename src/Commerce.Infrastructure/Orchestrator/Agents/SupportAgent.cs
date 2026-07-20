using Commerce.Application.Abstractions;
using Commerce.Domain;
using Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Commerce.Infrastructure.Orchestrator.Agents;

/// <summary>Ticket volumes, triage pipeline state and notable open tickets.</summary>
public sealed class SupportAgent(
    IDashboardQueries dashboard,
    CommerceDbContext db) : IBusinessAgent
{
    public string Domain => "support";

    public async Task<AgentContext> GatherAsync(AgentQuery query, CancellationToken ct = default)
    {
        DateTimeOffset to = DateTimeOffset.UtcNow;
        DateTimeOffset from = to.AddDays(-query.Days);
        var snapshot = await dashboard.GetSupportSnapshotAsync(from, to, ct);

        var open = await db.Tickets.AsNoTracking()
            .Where(t => t.Status == TicketStatus.Escalated
                        || (t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Discarded
                            && t.Urgency == TicketUrgency.High))
            .OrderByDescending(t => t.CreatedAt)
            .Take(8)
            .Select(t => new { t.Subject, t.Brand, t.Status, t.Urgency, t.AssignedTeam, t.Category })
            .ToListAsync(ct);

        var context = new StringBuilder();
        context.AppendLine(
            $"SUPPORT (last {query.Days} days): {snapshot.Created} tickets created · "
            + $"{snapshot.Queued} queued · {snapshot.Triaged} triaged · {snapshot.Escalated} escalated · "
            + $"{snapshot.Resolved} resolved · {snapshot.HighUrgency} high urgency"
            + (snapshot.AvgTriageMinutes is { } m ? $" · avg triage time {m:F1} min" : ""));

        if (open.Count > 0)
        {
            context.AppendLine("ESCALATED / HIGH-URGENCY OPEN TICKETS:\n");
            context.AppendLine("| Subject | Brand | Category | Status | Assigned Team |");
            context.AppendLine("|---|---|---|---|---|");
            foreach (var t in open)
                context.AppendLine(
                    $"| \"{t.Subject}\" | {t.Brand ?? "unknown"} | {t.Category?.ToString() ?? "unclassified"} | "
                    + $"{t.Status} | {(t.AssignedTeam is null ? "-" : t.AssignedTeam)} |");
            context.AppendLine();
        }

        var chart = new ChatChart("bar",
            ["Queued", "Triaged", "Escalated", "Resolved"],
            [new ChatChartSeries("Tickets",
                [snapshot.Queued, snapshot.Triaged, snapshot.Escalated, snapshot.Resolved])]);

        return new AgentContext(Domain, context.ToString(),
            Sources: [new ChatSource("Tickets", "database")],
            Actions: [new ActionLink("Open Support AI", "/support")],
            Chart: chart);
    }
}
