namespace Commerce.Application.Abstractions;

// ── Executive dashboard (Phase 4) ────────────────────────────────────────────

public sealed record SalesBucket(DateOnly Date, decimal Revenue, int Orders);

public sealed record DashboardSummary(
    decimal Revenue, decimal RevenuePrior, double RevenueGrowthPct,
    int OrderCount, int OrderCountPrior, double OrderGrowthPct,
    decimal AvgOrderValue, int ActiveCustomers,
    int InventoryHealthScore, int LowStockCount,
    int ActiveTickets, int EscalatedTickets,
    IReadOnlyList<SalesBucket> Sparkline);

public sealed record SalesTrend(
    string Granularity,
    IReadOnlyList<SalesBucket> Current,
    IReadOnlyList<SalesBucket> Prior);

public sealed record TopCustomer(
    Guid CustomerId, string Name, string Email, decimal TotalSpend, int OrderCount);

public sealed record TopProduct(
    Guid ProductId, string Name, string Brand, string Sku,
    int QuantitySold, decimal Revenue);

public sealed record SupportSnapshot(
    int Created, int Queued, int Triaged, int Escalated, int Resolved,
    int HighUrgency, double? AvgTriageMinutes);

public sealed record ActivityItem(
    string Type, string Title, string Description, string Severity,
    DateTimeOffset Timestamp, Guid? ReferenceId);

public sealed record AiFeatureUsage(
    string Feature, int Calls, long InputTokens, long OutputTokens,
    decimal CostUsd, double AvgLatencyMs, int Failures);

public sealed record AiUsageBucket(DateOnly Date, int Calls, decimal CostUsd);

public sealed record AiUsageStats(
    int TotalCalls, int Failures, long InputTokens, long OutputTokens,
    decimal CostUsd, double AvgLatencyMs,
    IReadOnlyList<AiFeatureUsage> ByFeature,
    IReadOnlyList<AiUsageBucket> ByDay);

public interface IDashboardQueries
{
    Task<DashboardSummary> GetSummaryAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<SalesTrend> GetSalesTrendAsync(DateTimeOffset from, DateTimeOffset to, string granularity, CancellationToken ct = default);
    Task<IReadOnlyList<TopCustomer>> GetTopCustomersAsync(DateTimeOffset from, DateTimeOffset to, int take, CancellationToken ct = default);
    Task<IReadOnlyList<TopProduct>> GetTopProductsAsync(DateTimeOffset from, DateTimeOffset to, int take, CancellationToken ct = default);
    Task<SupportSnapshot> GetSupportSnapshotAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<IReadOnlyList<ActivityItem>> GetRecentActivityAsync(int take, CancellationToken ct = default);
    Task<AiUsageStats> GetAiUsageAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}
