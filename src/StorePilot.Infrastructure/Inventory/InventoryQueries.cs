using StorePilot.Application.Abstractions;
using StorePilot.Application.Forecasting;
using StorePilot.Domain.Entities;
using StorePilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StorePilot.Infrastructure.Inventory;

public sealed class InventoryQueries(
    StorePilotDbContext db,
    InventorySnapshotCache cache,
    InventoryAnalysisService analysis) : IInventoryQueries
{
    public async Task<IReadOnlyList<InventoryProductRow>> ListProductsAsync(CancellationToken ct = default)
    {
        await EnsureSnapshotAsync(ct);
        return cache.Rows;
    }

    public async Task<HealthSummary> GetHealthAsync(CancellationToken ct = default)
    {
        await EnsureSnapshotAsync(ct);
        return cache.Health!;
    }

    public async Task<ForecastSeries?> GetForecastAsync(Guid productId, int horizonDays, CancellationToken ct = default)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (product is null) return null;

        horizonDays = horizonDays is 7 or 30 or 90 ? horizonDays : 30;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var sold = await db.OrderItems.AsNoTracking()
            .Where(i => i.ProductId == productId)
            .Join(db.Orders.AsNoTracking().Where(o => o.CreatedAt >= now.AddDays(-60)),
                i => i.OrderId, o => o.Id, (i, o) => new { o.CreatedAt, i.Quantity })
            .ToListAsync(ct);
        var byDay = sold.GroupBy(s => DateOnly.FromDateTime(s.CreatedAt.UtcDateTime))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var history = Enumerable.Range(0, 60)
            .Select(d => today.AddDays(-59 + d))
            .Select(date => new SeriesPoint(date, byDay.GetValueOrDefault(date)))
            .ToList();

        int[] daily = history.Select(h => h.Units).ToArray();
        double rate = ForecastMath.DailyRate(daily);
        double confidence = ForecastMath.Confidence(daily);

        // Forecast as cumulative-fair daily allocation of rate over the horizon.
        var forecast = new List<SeriesPoint>();
        double carry = 0;
        for (int d = 1; d <= horizonDays; d++)
        {
            carry += rate;
            int units = (int)Math.Floor(carry);
            carry -= units;
            forecast.Add(new SeriesPoint(today.AddDays(d), units));
        }

        return new ForecastSeries(
            product.Id, product.Sku, product.Name, product.Brand,
            history, forecast, Math.Round(rate, 2), Math.Round(confidence, 2), horizonDays);
    }

    public async Task<IReadOnlyList<InventoryAlert>> GetAlertsAsync(bool unacknowledgedOnly, CancellationToken ct = default)
    {
        IQueryable<InventoryAlert> query = db.InventoryAlerts.AsNoTracking();
        if (unacknowledgedOnly) query = query.Where(a => !a.Acknowledged);
        return await query.OrderByDescending(a => a.CreatedAt).Take(100).ToListAsync(ct);
    }

    public async Task<bool> AcknowledgeAlertAsync(Guid id, CancellationToken ct = default)
    {
        var alert = await db.InventoryAlerts.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (alert is null) return false;
        alert.Acknowledged = true;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<ReorderSuggestion>> GetReorderSuggestionsAsync(CancellationToken ct = default)
        => await db.ReorderSuggestions.AsNoTracking().OrderBy(r => r.OrderByDate).ToListAsync(ct);

    private async Task EnsureSnapshotAsync(CancellationToken ct)
    {
        if (cache.Health is null)
            await analysis.RunAsync(ct);
    }
}
