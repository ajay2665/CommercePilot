using Commerce.Application.Abstractions;
using Commerce.Infrastructure.Ai;
using System.Text;

namespace Commerce.Infrastructure.Orchestrator;

/// <summary>
/// Semantic access layer over existing data: compact per-domain snapshot lines
/// for LLM prompts (used by the DashboardAgent for cross-module summaries) and
/// vector search over the already-embedded catalog + knowledge articles.
/// </summary>
public sealed class GlobalKnowledgeBase(
    IDashboardQueries dashboard,
    IInventoryQueries inventory,
    EmbeddingService embeddings) : IGlobalKnowledgeBase
{
    public async Task<string> BuildContextAsync(
        IReadOnlyList<string> domains, int days, CancellationToken ct = default)
    {
        DateTimeOffset to = DateTimeOffset.UtcNow;
        DateTimeOffset from = to.AddDays(-days);
        var context = new StringBuilder();

        if (domains.Contains("sales") || domains.Contains("dashboard"))
        {
            var s = await dashboard.GetSummaryAsync(from, to, ct);
            context.AppendLine(
                $"SALES (last {days}d): revenue ${s.Revenue:F0} ({Signed(s.RevenueGrowthPct)}% vs prior {days}d, ${s.RevenuePrior:F0}) · "
                + $"{s.OrderCount} orders ({Signed(s.OrderGrowthPct)}%) · avg order ${s.AvgOrderValue:F2} · {s.ActiveCustomers} active customers");
        }

        if (domains.Contains("inventory") || domains.Contains("dashboard"))
        {
            var h = await inventory.GetHealthAsync(ct);
            context.AppendLine(
                $"INVENTORY: health {h.HealthScore}/100 · {h.TotalProducts} products · stock value ${h.TotalStockValue:F0} · "
                + $"{h.LowStockCount} low stock · {h.DeadCount} dead (${h.DeadValue:F0}) · {h.OverstockCount} overstock (${h.OverstockValue:F0})");
        }

        if (domains.Contains("support") || domains.Contains("dashboard"))
        {
            var t = await dashboard.GetSupportSnapshotAsync(from, to, ct);
            context.AppendLine(
                $"SUPPORT (last {days}d): {t.Created} tickets · {t.Queued} queued · {t.Triaged} triaged · "
                + $"{t.Escalated} escalated · {t.Resolved} resolved · {t.HighUrgency} high urgency"
                + (t.AvgTriageMinutes is { } m ? $" · avg triage {m:F1} min" : ""));
        }

        return context.ToString();
    }

    public async Task<IReadOnlyList<ChatSource>> SemanticSearchAsync(
        string query, int topK, CancellationToken ct = default)
    {
        var hits = await embeddings.SearchAsync(query, null, topK, ct);
        return hits
            .Select(h => new ChatSource(h.Title, h.SourceType.ToString()))
            .DistinctBy(s => s.Title)
            .ToList();
    }

    private static string Signed(double pct) => pct >= 0 ? $"+{pct:F1}" : $"{pct:F1}";
}
