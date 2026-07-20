using Commerce.Domain.Entities;

namespace Commerce.Application.Abstractions;

// ── Inventory pod ────────────────────────────────────────────────────────────

public sealed record InventoryProductRow(
    Guid ProductId, string Sku, string Name, string Brand, decimal Price,
    int CurrentStock, int SafetyStock, int ReorderPoint, int LeadTimeDays,
    double DailyRate, int Forecast30, int? DaysUntilStockout,
    StockClass Classification, decimal StockValue);

public sealed record HealthSummary(
    int HealthScore, int TotalProducts, decimal TotalStockValue,
    int FastCount, int SlowCount, int DeadCount, int OverstockCount, int LowStockCount,
    decimal DeadValue, decimal OverstockValue, DateTimeOffset GeneratedAt);

public sealed record SeriesPoint(DateOnly Date, int Units);

public sealed record ForecastSeries(
    Guid ProductId, string Sku, string Name, string Brand,
    IReadOnlyList<SeriesPoint> History, IReadOnlyList<SeriesPoint> Forecast,
    double DailyRate, double Confidence, int HorizonDays);

public interface IInventoryQueries
{
    Task<IReadOnlyList<InventoryProductRow>> ListProductsAsync(CancellationToken ct = default);
    Task<HealthSummary> GetHealthAsync(CancellationToken ct = default);
    Task<ForecastSeries?> GetForecastAsync(Guid productId, int horizonDays, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryAlert>> GetAlertsAsync(bool unacknowledgedOnly, CancellationToken ct = default);
    Task<bool> AcknowledgeAlertAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ReorderSuggestion>> GetReorderSuggestionsAsync(CancellationToken ct = default);
}

public sealed record CopilotAnswer(string Answer);

public interface IInventoryCopilot
{
    Task<CopilotAnswer> AskAsync(string question, CancellationToken ct = default);
}

// ── Shopping pod ─────────────────────────────────────────────────────────────

public sealed record ShopProductCard(
    Guid ProductId, string Sku, string Name, string Brand, decimal Price,
    bool InStock, int CurrentStock, double Score, string Reason);

public sealed record CustomerLite(Guid Id, string Name, string Email);

public interface IShoppingQueries
{
    Task<IReadOnlyList<ShopProductCard>> GetRecommendationsAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<ShopProductCard>> GetTrendingAsync(CancellationToken ct = default);
    Task TrackEventAsync(Guid customerId, Guid productId, ShopEventType type, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerLite>> GetCustomersAsync(CancellationToken ct = default);
}

public sealed record AssistantSource(string Title, string Kind);

public sealed record AssistantAnswer(string Answer, IReadOnlyList<AssistantSource> Sources);

public interface IShoppingAi
{
    Task<IReadOnlyList<ShopProductCard>> SearchAsync(string query, CancellationToken ct = default);
    Task<AssistantAnswer> AskAsync(string question, CancellationToken ct = default);
    Task<string> CompareAsync(IReadOnlyList<Guid> productIds, CancellationToken ct = default);
}
