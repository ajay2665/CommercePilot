using Commerce.Application.Abstractions;
using Commerce.Application.Options;
using Commerce.Domain.Entities;
using Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Commerce.Infrastructure.Shopping;

public sealed class ShoppingQueries(CommerceDbContext db, IOptions<ShoppingOptions> options) : IShoppingQueries
{
    public async Task<IReadOnlyList<ShopProductCard>> GetRecommendationsAsync(Guid customerId, CancellationToken ct = default)
    {
        var recs = await db.Recommendations.AsNoTracking()
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.Score)
            .Take(options.Value.RecommendationsPerCustomer)
            .ToListAsync(ct);

        if (recs.Count == 0)
            return await GetTrendingAsync(ct); // cold start: fall back to trending

        var cards = await ToCardsAsync(recs.Select(r => r.ProductId).ToList(), ct);
        return recs
            .Select(r => cards.GetValueOrDefault(r.ProductId) is { } c
                ? c with { Score = Math.Round(r.Score, 2), Reason = "Customers who bought your items also bought this" }
                : null)
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();
    }

    public async Task<IReadOnlyList<ShopProductCard>> GetTrendingAsync(CancellationToken ct = default)
    {
        DateTimeOffset since = DateTimeOffset.UtcNow.AddDays(-options.Value.TrendingWindowDays);
        var top = await db.OrderItems.AsNoTracking()
            .Join(db.Orders.AsNoTracking().Where(o => o.CreatedAt >= since),
                i => i.OrderId, o => o.Id, (i, _) => i)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Units = g.Sum(i => i.Quantity) })
            .OrderByDescending(x => x.Units)
            .Take(8)
            .ToListAsync(ct);

        var cards = await ToCardsAsync(top.Select(t => t.ProductId).ToList(), ct);
        return top
            .Where(t => cards.ContainsKey(t.ProductId))
            .Select(t => cards[t.ProductId] with { Score = t.Units, Reason = $"{t.Units} sold in the last {options.Value.TrendingWindowDays} days" })
            .ToList();
    }

    public async Task TrackEventAsync(Guid customerId, Guid productId, ShopEventType type, CancellationToken ct = default)
    {
        db.CustomerEvents.Add(new CustomerEvent { CustomerId = customerId, ProductId = productId, EventType = type });

        if (type == ShopEventType.Cart)
        {
            var cart = await db.AbandonedCarts
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && !c.RecoveryEmailSent, ct);
            if (cart is null)
            {
                cart = new AbandonedCart { CustomerId = customerId };
                db.AbandonedCarts.Add(cart);
            }
            var ids = JsonSerializer.Deserialize<List<Guid>>(cart.ProductIdsJson) ?? [];
            if (!ids.Contains(productId)) ids.Add(productId);
            cart.ProductIdsJson = JsonSerializer.Serialize(ids);
            cart.LastActiveAt = DateTimeOffset.UtcNow;
        }
        else if (type == ShopEventType.Purchase)
        {
            var carts = await db.AbandonedCarts.Where(c => c.CustomerId == customerId).ToListAsync(ct);
            db.AbandonedCarts.RemoveRange(carts);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CustomerLite>> GetCustomersAsync(CancellationToken ct = default)
        => await db.Customers.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CustomerLite(c.Id, c.Name, c.Email))
            .Take(50)
            .ToListAsync(ct);

    internal async Task<Dictionary<Guid, ShopProductCard>> ToCardsAsync(List<Guid> productIds, CancellationToken ct)
    {
        var rows = await db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .GroupJoin(db.InventoryLevels.AsNoTracking(), p => p.Id, i => i.ProductId,
                (p, levels) => new { p, stock = levels.Select(l => (int?)l.CurrentStock).FirstOrDefault() })
            .ToListAsync(ct);

        return rows.ToDictionary(
            x => x.p.Id,
            x => new ShopProductCard(
                x.p.Id, x.p.Sku, x.p.Name, x.p.Brand, x.p.Price,
                InStock: (x.stock ?? 0) > 0, CurrentStock: x.stock ?? 0, Score: 0, Reason: ""));
    }
}

/// <summary>
/// "Customers who bought X also bought Y" — pure co-occurrence over order
/// history (decision 7), materialised into the Recommendations table.
/// </summary>
public static class RecommendationBuilder
{
    public static async Task<int> RebuildAsync(CommerceDbContext db, int perCustomer, ILogger logger, CancellationToken ct)
    {
        var orders = await db.Orders.AsNoTracking()
            .Join(db.OrderItems.AsNoTracking(), o => o.Id, i => i.OrderId,
                (o, i) => new { o.Id, o.CustomerId, i.ProductId })
            .ToListAsync(ct);

        var basketsByOrder = orders.GroupBy(x => x.Id)
            .Select(g => g.Select(x => x.ProductId).Distinct().ToArray())
            .Where(b => b.Length > 1)
            .ToList();

        // Co-occurrence counts across multi-item orders.
        var cooc = new Dictionary<(Guid A, Guid B), int>();
        foreach (var basket in basketsByOrder)
            for (int a = 0; a < basket.Length; a++)
                for (int b = 0; b < basket.Length; b++)
                    if (a != b)
                        cooc[(basket[a], basket[b])] = cooc.GetValueOrDefault((basket[a], basket[b])) + 1;

        var owned = orders.GroupBy(x => x.CustomerId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ProductId).ToHashSet());

        var recommendations = new List<Recommendation>();
        foreach (var (customerId, ownedSet) in owned)
        {
            var scores = new Dictionary<Guid, double>();
            foreach (var product in ownedSet)
                foreach (var ((a, b), count) in cooc)
                    if (a == product && !ownedSet.Contains(b))
                        scores[b] = scores.GetValueOrDefault(b) + count;

            recommendations.AddRange(scores
                .OrderByDescending(s => s.Value)
                .Take(perCustomer)
                .Select(s => new Recommendation { CustomerId = customerId, ProductId = s.Key, Score = s.Value }));
        }

        await db.Recommendations.ExecuteDeleteAsync(ct);
        db.Recommendations.AddRange(recommendations);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Recommendations rebuilt: {Count} rows for {Customers} customers",
            recommendations.Count, owned.Count);
        return recommendations.Count;
    }
}
