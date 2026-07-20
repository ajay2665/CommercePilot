using Commerce.Application.Abstractions;
using System.Text;

namespace Commerce.Infrastructure.Orchestrator.Agents;

/// <summary>Revenue, orders, growth, top products and top customers.</summary>
public sealed class SalesAgent(IDashboardQueries dashboard) : IBusinessAgent
{
    public string Domain => "sales";

    public async Task<AgentContext> GatherAsync(AgentQuery query, CancellationToken ct = default)
    {
        DateTimeOffset to = DateTimeOffset.UtcNow;
        DateTimeOffset from = to.AddDays(-query.Days);

        var summary = await dashboard.GetSummaryAsync(from, to, ct);
        string granularity = query.Days > 60 ? "week" : "day";
        var trend = await dashboard.GetSalesTrendAsync(from, to, granularity, ct);
        var topProducts = await dashboard.GetTopProductsAsync(from, to, 5, ct);
        var topCustomers = await dashboard.GetTopCustomersAsync(from, to, 5, ct);

        var context = new StringBuilder();
        context.AppendLine(
            $"SALES SUMMARY (last {query.Days} days vs the {query.Days} days before):");
        context.AppendLine(
            $"  Revenue ${summary.Revenue:F2} vs prior ${summary.RevenuePrior:F2} ({summary.RevenueGrowthPct:+0.0;-0.0}%)");
        context.AppendLine(
            $"  Orders {summary.OrderCount} vs prior {summary.OrderCountPrior} ({summary.OrderGrowthPct:+0.0;-0.0}%)");
        context.AppendLine(
            $"  Avg order value ${summary.AvgOrderValue:F2} · {summary.ActiveCustomers} active customers");

        context.AppendLine("TOP PRODUCTS BY UNITS:\n");
        context.AppendLine("| Product | SKU | Brand | Units Sold | Revenue |");
        context.AppendLine("|---|---|---|---|---|");
        foreach (var p in topProducts)
            context.AppendLine($"| {p.Name} | {p.Sku} | {p.Brand} | {p.QuantitySold} | ${p.Revenue:F2} |");
        context.AppendLine();

        context.AppendLine("TOP CUSTOMERS BY SPEND:\n");
        context.AppendLine("| Name | Email | Total Spend | Orders |");
        context.AppendLine("|---|---|---|---|");
        foreach (var c in topCustomers)
            context.AppendLine($"| {c.Name} | {c.Email} | ${c.TotalSpend:F2} | {c.OrderCount} |");
        context.AppendLine();

        var chart = new ChatChart("area",
            trend.Current.Select(b => b.Date.ToString("MMM d")).ToList(),
            [new ChatChartSeries("Revenue", trend.Current.Select(b => Math.Round(b.Revenue, 2)).ToList())]);

        return new AgentContext(Domain, context.ToString(),
            Sources: [new ChatSource("Orders", "database")],
            Actions: [new ActionLink("Open dashboard", "/")],
            Chart: chart);
    }
}
