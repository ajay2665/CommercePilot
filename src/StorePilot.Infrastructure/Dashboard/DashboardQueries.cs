using StorePilot.Application.Abstractions;
using StorePilot.Domain;
using StorePilot.Domain.Entities;
using StorePilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StorePilot.Infrastructure.Dashboard;

/// <summary>
/// Read-only aggregations behind /api/dashboard/*. Row volumes are small
/// (seeded demo scale), so buckets and averages are computed in memory after a
/// narrow projection — no snapshot caches or materialized views needed yet.
/// </summary>
public sealed class DashboardQueries(
    StorePilotDbContext db,
    IInventoryQueries inventory) : IDashboardQueries
{
    public async Task<DashboardSummary> GetSummaryAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        TimeSpan span = to - from;
        DateTimeOffset priorFrom = from - span;

        var current = await OrdersInRange(from, to).ToListAsync(ct);
        var prior = await OrdersInRange(priorFrom, from).ToListAsync(ct);

        decimal revenue = current.Sum(o => o.Total);
        decimal revenuePrior = prior.Sum(o => o.Total);

        var health = await inventory.GetHealthAsync(ct);

        int activeTickets = await db.Tickets.AsNoTracking().CountAsync(
            t => t.Status == TicketStatus.Queued || t.Status == TicketStatus.Triaged
                 || t.Status == TicketStatus.Escalated, ct);
        int escalated = await db.Tickets.AsNoTracking()
            .CountAsync(t => t.Status == TicketStatus.Escalated, ct);

        return new DashboardSummary(
            Revenue: revenue,
            RevenuePrior: revenuePrior,
            RevenueGrowthPct: GrowthPct((double)revenue, (double)revenuePrior),
            OrderCount: current.Count,
            OrderCountPrior: prior.Count,
            OrderGrowthPct: GrowthPct(current.Count, prior.Count),
            AvgOrderValue: current.Count == 0 ? 0 : Math.Round(revenue / current.Count, 2),
            ActiveCustomers: current.Select(o => o.CustomerId).Distinct().Count(),
            InventoryHealthScore: health.HealthScore,
            LowStockCount: health.LowStockCount,
            ActiveTickets: activeTickets,
            EscalatedTickets: escalated,
            Sparkline: Bucket(current, from, to, "day"));
    }

    public async Task<SalesTrend> GetSalesTrendAsync(
        DateTimeOffset from, DateTimeOffset to, string granularity, CancellationToken ct = default)
    {
        granularity = granularity is "week" or "month" ? granularity : "day";
        TimeSpan span = to - from;
        DateTimeOffset priorFrom = from - span;

        var current = await OrdersInRange(from, to).ToListAsync(ct);
        var prior = await OrdersInRange(priorFrom, from).ToListAsync(ct);

        return new SalesTrend(granularity,
            Bucket(current, from, to, granularity),
            Bucket(prior, priorFrom, from, granularity));
    }

    public async Task<IReadOnlyList<TopCustomer>> GetTopCustomersAsync(
        DateTimeOffset from, DateTimeOffset to, int take, CancellationToken ct = default)
    {
        var top = await OrdersInRange(from, to)
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Spend = g.Sum(o => o.Total), Orders = g.Count() })
            .OrderByDescending(x => x.Spend)
            .Take(take)
            .ToListAsync(ct);

        var ids = top.Select(t => t.CustomerId).ToList();
        var customers = await db.Customers.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        return top.Select(t =>
            {
                var c = customers.GetValueOrDefault(t.CustomerId);
                return new TopCustomer(t.CustomerId, c?.Name ?? "Unknown", c?.Email ?? "", t.Spend, t.Orders);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<TopProduct>> GetTopProductsAsync(
        DateTimeOffset from, DateTimeOffset to, int take, CancellationToken ct = default)
    {
        var top = await db.OrderItems.AsNoTracking()
            .Join(OrdersInRange(from, to), i => i.OrderId, o => o.Id, (i, o) => i)
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Quantity = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.Quantity * i.UnitPrice),
            })
            .OrderByDescending(x => x.Quantity)
            .Take(take)
            .ToListAsync(ct);

        var ids = top.Select(t => t.ProductId).ToList();
        var products = await db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        return top.Select(t =>
            {
                var p = products.GetValueOrDefault(t.ProductId);
                return new TopProduct(
                    t.ProductId, p?.Name ?? "Unknown", p?.Brand ?? "", p?.Sku ?? "",
                    t.Quantity, t.Revenue);
            })
            .ToList();
    }

    public async Task<SupportSnapshot> GetSupportSnapshotAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var tickets = await db.Tickets.AsNoTracking()
            .Where(t => t.CreatedAt >= from && t.CreatedAt < to)
            .Select(t => new { t.Status, t.Urgency, t.CreatedAt, t.TriagedAt })
            .ToListAsync(ct);

        var triageTimes = tickets
            .Where(t => t.TriagedAt is not null)
            .Select(t => (t.TriagedAt!.Value - t.CreatedAt).TotalMinutes)
            .ToList();

        return new SupportSnapshot(
            Created: tickets.Count,
            Queued: tickets.Count(t => t.Status == TicketStatus.Queued),
            Triaged: tickets.Count(t => t.Status == TicketStatus.Triaged),
            Escalated: tickets.Count(t => t.Status == TicketStatus.Escalated),
            Resolved: tickets.Count(t => t.Status == TicketStatus.Resolved),
            HighUrgency: tickets.Count(t => t.Urgency == TicketUrgency.High),
            AvgTriageMinutes: triageTimes.Count == 0 ? null : Math.Round(triageTimes.Average(), 1));
    }

    public async Task<IReadOnlyList<ActivityItem>> GetRecentActivityAsync(
        int take, CancellationToken ct = default)
    {
        var items = new List<ActivityItem>();

        var orders = await db.Orders.AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Take(take)
            .Join(db.Customers.AsNoTracking(), o => o.CustomerId, c => c.Id,
                (o, c) => new { o.Id, o.Total, o.CreatedAt, Customer = c.Name })
            .ToListAsync(ct);
        items.AddRange(orders.Select(o => new ActivityItem(
            "order", $"Order — ${o.Total:F2}", $"Placed by {o.Customer}", "info", o.CreatedAt, o.Id)));

        var tickets = await db.Tickets.AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Take(take)
            .Select(t => new { t.Id, t.Subject, t.Status, t.Urgency, t.CreatedAt })
            .ToListAsync(ct);
        items.AddRange(tickets.Select(t => new ActivityItem(
            "ticket", t.Subject,
            $"Support ticket · {t.Status}" + (t.Urgency is null ? "" : $" · {t.Urgency} urgency"),
            t.Status == TicketStatus.Escalated ? "critical"
                : t.Urgency == TicketUrgency.High ? "warning" : "info",
            t.CreatedAt, t.Id)));

        var alerts = await db.InventoryAlerts.AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
        items.AddRange(alerts.Select(a => new ActivityItem(
            "inventory", $"{a.Type} alert", a.Message,
            a.Severity.ToString().ToLowerInvariant(), a.CreatedAt, a.Id)));

        var notifications = await db.Notifications.AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
        items.AddRange(notifications.Select(n => new ActivityItem(
            "notification", n.Title, n.Message,
            n.Severity.ToString().ToLowerInvariant(), n.CreatedAt, n.Id)));

        return items.OrderByDescending(i => i.Timestamp).Take(take).ToList();
    }

    public async Task<AiUsageStats> GetAiUsageAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var calls = await db.AiInteractions.AsNoTracking()
            .Where(a => a.CreatedAt >= from && a.CreatedAt < to)
            .Select(a => new
            {
                a.Feature,
                a.InputTokens,
                a.OutputTokens,
                a.LatencyMs,
                a.CostUsd,
                a.Success,
                a.CreatedAt,
            })
            .ToListAsync(ct);

        var byFeature = calls
            .GroupBy(c => c.Feature)
            .Select(g => new AiFeatureUsage(
                g.Key, g.Count(),
                g.Sum(c => (long)c.InputTokens), g.Sum(c => (long)c.OutputTokens),
                g.Sum(c => c.CostUsd), Math.Round(g.Average(c => (double)c.LatencyMs), 0),
                g.Count(c => !c.Success)))
            .OrderByDescending(f => f.Calls)
            .ToList();

        var byDay = calls
            .GroupBy(c => DateOnly.FromDateTime(c.CreatedAt.UtcDateTime))
            .OrderBy(g => g.Key)
            .Select(g => new AiUsageBucket(g.Key, g.Count(), g.Sum(c => c.CostUsd)))
            .ToList();

        return new AiUsageStats(
            TotalCalls: calls.Count,
            Failures: calls.Count(c => !c.Success),
            InputTokens: calls.Sum(c => (long)c.InputTokens),
            OutputTokens: calls.Sum(c => (long)c.OutputTokens),
            CostUsd: calls.Sum(c => c.CostUsd),
            AvgLatencyMs: calls.Count == 0 ? 0 : Math.Round(calls.Average(c => (double)c.LatencyMs), 0),
            ByFeature: byFeature,
            ByDay: byDay);
    }

    private IQueryable<Order> OrdersInRange(DateTimeOffset from, DateTimeOffset to)
        => db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= from && o.CreatedAt < to && o.Status != OrderStatus.Cancelled);

    private static double GrowthPct(double current, double prior)
        => prior == 0 ? (current > 0 ? 100 : 0) : Math.Round((current - prior) / prior * 100, 1);

    /// <summary>Groups orders into contiguous day/week/month buckets, zero-filling gaps.</summary>
    private static List<SalesBucket> Bucket(
        IEnumerable<Order> orders, DateTimeOffset from, DateTimeOffset to, string granularity)
    {
        var start = BucketKey(DateOnly.FromDateTime(from.UtcDateTime), granularity);
        var end = DateOnly.FromDateTime(to.UtcDateTime);

        var grouped = orders
            .GroupBy(o => BucketKey(DateOnly.FromDateTime(o.CreatedAt.UtcDateTime), granularity))
            .ToDictionary(g => g.Key, g => (Revenue: g.Sum(o => o.Total), Orders: g.Count()));

        var buckets = new List<SalesBucket>();
        for (var key = start; key <= end; key = Advance(key, granularity))
        {
            var value = grouped.GetValueOrDefault(key);
            buckets.Add(new SalesBucket(key, value.Revenue, value.Orders));
        }
        return buckets;
    }

    private static DateOnly BucketKey(DateOnly date, string granularity) => granularity switch
    {
        "week" => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)), // Monday of that week
        "month" => new DateOnly(date.Year, date.Month, 1),
        _ => date,
    };

    private static DateOnly Advance(DateOnly date, string granularity) => granularity switch
    {
        "week" => date.AddDays(7),
        "month" => date.AddMonths(1),
        _ => date.AddDays(1),
    };
}
