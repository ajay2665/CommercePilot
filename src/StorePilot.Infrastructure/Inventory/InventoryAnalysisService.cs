using StorePilot.Application.Abstractions;
using StorePilot.Application.Events;
using StorePilot.Application.Forecasting;
using StorePilot.Application.Options;
using StorePilot.Domain;
using StorePilot.Domain.Entities;
using StorePilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StorePilot.Infrastructure.Inventory;

/// <summary>Latest analysis snapshot, shared by queries, the copilot, and the health endpoint.</summary>
public sealed class InventorySnapshotCache
{
    public IReadOnlyList<InventoryProductRow> Rows { get; internal set; } = [];
    public HealthSummary? Health { get; internal set; }
}

/// <summary>
/// The deterministic analysis pass (decision 7 — math, not LLM): daily sales →
/// forecast snapshots, stock classification, health score, alerts, and reorder
/// suggestions. Run by the worker at startup and on an interval.
/// </summary>
public sealed class InventoryAnalysisService(
    StorePilotDbContext db,
    InventorySnapshotCache cache,
    IEventBus bus,
    IOptions<InventoryOptions> options,
    ILogger<InventoryAnalysisService> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var opt = options.Value;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset windowStart = now.AddDays(-90);

        var products = await db.Products.AsNoTracking().ToListAsync(ct);
        var inventory = (await db.InventoryLevels.ToListAsync(ct)).ToDictionary(i => i.ProductId);
        var suppliers = await db.Suppliers.AsNoTracking().OrderBy(s => s.AvgLeadTimeDays).ToListAsync(ct);

        var sales = await db.OrderItems.AsNoTracking()
            .Join(db.Orders.AsNoTracking().Where(o => o.CreatedAt >= windowStart),
                i => i.OrderId, o => o.Id,
                (i, o) => new { i.ProductId, o.CreatedAt, i.Quantity })
            .ToListAsync(ct);

        var salesByProduct = sales.GroupBy(s => s.ProductId).ToDictionary(
            g => g.Key,
            g => g.GroupBy(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime))
                  .ToDictionary(d => d.Key, d => d.Sum(x => x.Quantity)));

        // Dense 90-day daily series per product (gaps = 0), plus rate stats.
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var series = new Dictionary<Guid, int[]>();
        foreach (var product in products)
        {
            var daily = new int[90];
            if (salesByProduct.TryGetValue(product.Id, out var byDay))
                for (int d = 0; d < 90; d++)
                    daily[d] = byDay.GetValueOrDefault(today.AddDays(-89 + d));
            series[product.Id] = daily;
        }
        var rates = products.ToDictionary(p => p.Id, p => ForecastMath.DailyRate(series[p.Id]));
        double[] positiveRates = [.. rates.Values.Where(r => r > 0).OrderBy(r => r)];
        double p75 = Percentile(positiveRates, 75);
        double p25 = Percentile(positiveRates, 25);

        var rows = new List<InventoryProductRow>();
        var forecasts = new List<DemandForecast>();
        var suggestions = new List<ReorderSuggestion>();
        var unacknowledged = (await db.InventoryAlerts.Where(a => !a.Acknowledged).ToListAsync(ct))
            .ToLookup(a => (a.ProductId, a.Type));

        foreach (var product in products)
        {
            if (!inventory.TryGetValue(product.Id, out var level)) continue;

            int[] daily = series[product.Id];
            double rate = rates[product.Id];
            double confidence = ForecastMath.Confidence(daily);
            int? stockoutDays = ForecastMath.DaysUntilStockout(level.CurrentStock, level.SafetyStock, rate);
            bool hasRecentSales = daily.TakeLast(opt.DeadStockDays).Any(u => u > 0);

            StockClass cls =
                level.CurrentStock <= level.ReorderPoint ? StockClass.LowStock
                : !hasRecentSales && level.CurrentStock > 0 ? StockClass.Dead
                : rate > 0 && level.CurrentStock > rate * opt.OverstockDaysOfDemand ? StockClass.Overstock
                : rate > 0 && rate >= p75 ? StockClass.Fast
                : rate > 0 && rate <= p25 ? StockClass.Slow
                : StockClass.Healthy;

            foreach (int horizon in (int[])[7, 30, 90])
                forecasts.Add(new DemandForecast
                {
                    ProductId = product.Id,
                    HorizonDays = horizon,
                    DailyRate = Math.Round(rate, 3),
                    PredictedUnits = ForecastMath.PredictUnits(rate, horizon),
                    Confidence = Math.Round(confidence, 2),
                });

            rows.Add(new InventoryProductRow(
                product.Id, product.Sku, product.Name, product.Brand, product.Price,
                level.CurrentStock, level.SafetyStock, level.ReorderPoint, level.LeadTimeDays,
                Math.Round(rate, 2), ForecastMath.PredictUnits(rate, 30), stockoutDays,
                cls, product.Price * level.CurrentStock));

            // Alerts — deduped against open (unacknowledged) ones, StockLow published once.
            async Task RaiseAsync(AlertType type, NotificationSeverity severity, string message)
            {
                if (unacknowledged[(product.Id, type)].Any()) return;
                db.InventoryAlerts.Add(new InventoryAlert
                {
                    ProductId = product.Id,
                    Type = type,
                    Severity = severity,
                    Message = message,
                });
                if (type == AlertType.LowStock)
                    await bus.PublishAsync(new StockLow(
                        product.Id, product.Name, product.Brand, level.CurrentStock, level.ReorderPoint), ct);
            }

            if (cls == StockClass.LowStock)
                await RaiseAsync(AlertType.LowStock, NotificationSeverity.Critical,
                    $"{product.Name} ({product.Sku}) at {level.CurrentStock} units — reorder point {level.ReorderPoint}.");
            else if (stockoutDays is { } days && days <= level.LeadTimeDays)
                await RaiseAsync(AlertType.StockoutRisk, NotificationSeverity.Warning,
                    $"{product.Name} ({product.Sku}) runs out in ~{days} days — lead time is {level.LeadTimeDays} days.");
            if (cls == StockClass.Dead)
                await RaiseAsync(AlertType.DeadStock, NotificationSeverity.Warning,
                    $"{product.Name} ({product.Sku}): no sales in {opt.DeadStockDays} days, {level.CurrentStock} units locked up.");
            if (cls == StockClass.Overstock)
                await RaiseAsync(AlertType.Overstock, NotificationSeverity.Info,
                    $"{product.Name} ({product.Sku}): {level.CurrentStock} units ≈ {(rate > 0 ? (int)(level.CurrentStock / rate) : 999)}+ days of demand.");

            // Smart reorder: low stock or stockout inside lead time.
            if (cls == StockClass.LowStock || (stockoutDays is { } d2 && d2 <= level.LeadTimeDays))
            {
                int quantity = ForecastMath.ReorderQuantity(level.CurrentStock, level.SafetyStock, level.LeadTimeDays, rate);
                if (quantity > 0)
                {
                    var supplier = suppliers.FirstOrDefault(s => s.AvgLeadTimeDays <= level.LeadTimeDays) ?? suppliers.FirstOrDefault();
                    suggestions.Add(new ReorderSuggestion
                    {
                        ProductId = product.Id,
                        SupplierId = supplier?.Id,
                        Quantity = quantity,
                        OrderByDate = now.AddDays(Math.Max(0, (stockoutDays ?? 0) - level.LeadTimeDays)),
                        Rationale = $"~{rate:F1}/day demand, {level.CurrentStock} on hand, lead time {level.LeadTimeDays}d"
                                  + (supplier is null ? "" : $" via {supplier.Name} ({supplier.AvgLeadTimeDays}d)"),
                    });
                }
            }
        }

        // Replace forecast + suggestion snapshots wholesale each run.
        await db.DemandForecasts.ExecuteDeleteAsync(ct);
        await db.ReorderSuggestions.ExecuteDeleteAsync(ct);
        db.DemandForecasts.AddRange(forecasts);
        db.ReorderSuggestions.AddRange(suggestions);
        await db.SaveChangesAsync(ct);

        decimal totalValue = rows.Sum(r => r.StockValue);
        decimal deadValue = rows.Where(r => r.Classification == StockClass.Dead).Sum(r => r.StockValue);
        decimal overValue = rows.Where(r => r.Classification == StockClass.Overstock).Sum(r => r.StockValue);
        int dead = rows.Count(r => r.Classification == StockClass.Dead);
        int over = rows.Count(r => r.Classification == StockClass.Overstock);
        int low = rows.Count(r => r.Classification == StockClass.LowStock);
        int slow = rows.Count(r => r.Classification == StockClass.Slow);
        double penalty = rows.Count == 0 ? 0
            : (dead * 1.0 + low * 0.8 + over * 0.6 + slow * 0.2) / rows.Count;

        cache.Rows = rows.OrderBy(r => r.Classification == StockClass.LowStock ? 0 : 1)
                         .ThenByDescending(r => r.StockValue).ToList();
        cache.Health = new HealthSummary(
            HealthScore: Math.Clamp((int)Math.Round(100 * (1 - penalty)), 5, 100),
            TotalProducts: rows.Count,
            TotalStockValue: totalValue,
            FastCount: rows.Count(r => r.Classification == StockClass.Fast),
            SlowCount: slow, DeadCount: dead, OverstockCount: over, LowStockCount: low,
            DeadValue: deadValue, OverstockValue: overValue,
            GeneratedAt: now);

        logger.LogInformation(
            "Inventory analysis: {Count} products — health {Score}, low {Low}, dead {Dead}, overstock {Over}, {Suggestions} reorder suggestion(s)",
            rows.Count, cache.Health.HealthScore, low, dead, over, suggestions.Count);
    }

    private static double Percentile(double[] sortedAscending, int percentile)
    {
        if (sortedAscending.Length == 0) return 0;
        double rank = (percentile / 100.0) * (sortedAscending.Length - 1);
        int lower = (int)Math.Floor(rank);
        int upper = (int)Math.Ceiling(rank);
        return sortedAscending[lower] + (sortedAscending[upper] - sortedAscending[lower]) * (rank - lower);
    }
}

/// <summary>StockLow → team notification, same INotifier path the Support pod uses.</summary>
public sealed class StockLowNotificationHandler(INotifier notifier) : IEventHandler<StockLow>
{
    public Task HandleAsync(StockLow @event, CancellationToken ct)
        => notifier.NotifyAsync(new NotificationMessage(
            Channel: "Inventory Alerts",
            Title: $"LOW STOCK [{@event.Brand}] {@event.ProductName}",
            Message: $"{@event.CurrentStock} units left (reorder point {@event.ReorderPoint}).",
            Severity: NotificationSeverity.Critical), ct);
}
