using Commerce.Application.Abstractions;
using Commerce.Domain.Entities;
using System.Text;

namespace Commerce.Infrastructure.Orchestrator.Agents;

/// <summary>
/// Stock health, risk lists and reorder suggestions — the same deterministic
/// snapshot the Inventory Copilot uses, exposed as a context block.
/// </summary>
public sealed class InventoryAgent(IInventoryQueries inventory) : IBusinessAgent
{
    public string Domain => "inventory";

    public async Task<AgentContext> GatherAsync(AgentQuery query, CancellationToken ct = default)
    {
        var health = await inventory.GetHealthAsync(ct);
        var rows = await inventory.ListProductsAsync(ct);
        var reorders = await inventory.GetReorderSuggestionsAsync(ct);

        var context = new StringBuilder();
        context.AppendLine(
            $"INVENTORY HEALTH: score {health.HealthScore}/100 · {health.TotalProducts} products · "
            + $"stock value ${health.TotalStockValue:F0} · fast {health.FastCount} · slow {health.SlowCount} · "
            + $"dead {health.DeadCount} (${health.DeadValue:F0} locked) · overstock {health.OverstockCount} "
            + $"(${health.OverstockValue:F0}) · low stock {health.LowStockCount}");

        AppendSection(context, "LOW STOCK (at/below reorder point)",
            rows.Where(r => r.Classification == StockClass.LowStock),
            ["Product", "SKU", "Brand", "On Hand", "Reorder Pt", "Est. Stockout"],
            r => $"{r.Name} | {r.Sku} | {r.Brand} | {r.CurrentStock} | {r.ReorderPoint} | {(r.DaysUntilStockout is { } d ? $"~{d} days" : "-")}");
        AppendSection(context, "DEAD STOCK (no sales in 60d)",
            rows.Where(r => r.Classification == StockClass.Dead),
            ["Product", "SKU", "Brand", "On Hand", "Value Locked"],
            r => $"{r.Name} | {r.Sku} | {r.Brand} | {r.CurrentStock} | ${r.StockValue:F0}");
        AppendSection(context, "TOP MOVERS",
            rows.Where(r => r.Classification == StockClass.Fast).OrderByDescending(r => r.DailyRate),
            ["Product", "SKU", "Brand", "Daily Rate", "30d Forecast", "On Hand"],
            r => $"{r.Name} | {r.Sku} | {r.Brand} | {r.DailyRate:F1}/day | {r.Forecast30} | {r.CurrentStock}");

        if (reorders.Count > 0)
        {
            var names = rows.ToDictionary(r => r.ProductId, r => $"{r.Name} [{r.Sku}]");
            context.AppendLine("REORDER SUGGESTIONS:\n");
            context.AppendLine("| Product | Order Qty | Order By | Rationale |");
            context.AppendLine("|---|---|---|---|");
            foreach (var s in reorders.Take(8))
                context.AppendLine(
                    $"| {names.GetValueOrDefault(s.ProductId, s.ProductId.ToString())} | {s.Quantity} "
                    + $"| {s.OrderByDate:yyyy-MM-dd} | {s.Rationale} |");
            context.AppendLine();
        }

        var chart = new ChatChart("doughnut",
            ["Healthy", "Fast", "Slow", "Dead", "Overstock", "Low stock"],
            [new ChatChartSeries("Products",
            [
                health.TotalProducts - health.FastCount - health.SlowCount - health.DeadCount
                    - health.OverstockCount - health.LowStockCount,
                health.FastCount, health.SlowCount, health.DeadCount,
                health.OverstockCount, health.LowStockCount,
            ])]);

        return new AgentContext(Domain, context.ToString(),
            Sources: [new ChatSource("Inventory analysis snapshot", "analysis")],
            Actions: [new ActionLink("Open Inventory AI", "/inventory")],
            Chart: chart);
    }

    private static void AppendSection(
        StringBuilder context, string title,
        IEnumerable<InventoryProductRow> rows, string[] headers, Func<InventoryProductRow, string> rowLine)
    {
        var list = rows.Take(8).ToList();
        if (list.Count == 0) return;
        context.AppendLine($"{title}:\n");
        context.AppendLine($"| {string.Join(" | ", headers)} |");
        context.AppendLine($"| {string.Join(" | ", headers.Select(_ => "---"))} |");
        foreach (var row in list)
            context.AppendLine($"| {rowLine(row)} |");
        context.AppendLine();
    }
}
