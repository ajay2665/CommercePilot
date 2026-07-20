using StorePilot.Application.Abstractions;
using StorePilot.Application.Features.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StorePilot.Infrastructure.Workers;

/// <summary>Surfaced by GET /api/status for the dashboard header pill.</summary>
public sealed class TriageStatus
{
    public Guid? CurrentTicketId { get; internal set; }
    public string? CurrentSubject { get; internal set; }
    public string? Stage { get; internal set; }
}

/// <summary>
/// SupportDeskAI's TicketWorker pattern: single consumer draining the intake
/// queue, one DI scope per ticket, failures logged and dropped (never re-queued).
/// </summary>
public sealed class TriageWorker(
    IWorkQueue<TriageWorkItem> queue,
    IServiceScopeFactory scopeFactory,
    TriageStatus status,
    ILogger<TriageWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RequeueOrphansAsync(stoppingToken);
        logger.LogInformation("TriageWorker started — waiting for tickets");

        await foreach (TriageWorkItem item in queue.DequeueAllAsync(stoppingToken))
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            try
            {
                var tickets = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
                var ticket = await tickets.GetAsync(item.TicketId, stoppingToken);

                status.CurrentTicketId = item.TicketId;
                status.CurrentSubject = ticket?.Subject;
                status.Stage = "Triage";

                var processor = scope.ServiceProvider.GetRequiredService<TriageProcessor>();
                await processor.ProcessAsync(item.TicketId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Triage pipeline failed for ticket {TicketId}", item.TicketId);
            }
            finally
            {
                status.CurrentTicketId = null;
                status.CurrentSubject = null;
                status.Stage = null;
            }
        }
    }

    /// <summary>
    /// The intake queue is in-memory, so tickets left Queued by a restart would
    /// otherwise be stranded — re-enqueue them before draining new work.
    /// </summary>
    private async Task RequeueOrphansAsync(CancellationToken ct)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var tickets = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
        var orphans = await tickets.ListAsync(
            new TicketFilter(Status: StorePilot.Domain.TicketStatus.Queued, PageSize: 100), ct);

        foreach (var ticket in orphans.Items)
            queue.Enqueue(new TriageWorkItem(ticket.Id));

        if (orphans.Items.Count > 0)
            logger.LogInformation("Re-queued {Count} ticket(s) left Queued by a previous run", orphans.Items.Count);
    }
}
