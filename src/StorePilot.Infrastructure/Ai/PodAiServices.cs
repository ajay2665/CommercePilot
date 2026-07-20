using StorePilot.Application.Abstractions;
using StorePilot.Application.Ai.Prompts;
using StorePilot.Domain;
using StorePilot.Domain.Entities;
using StorePilot.Infrastructure.Inventory;
using StorePilot.Infrastructure.Persistence;
using StorePilot.Infrastructure.Shopping;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace StorePilot.Infrastructure.Ai;

/// <summary>Semantic search, RAG assistant, and comparison for the Shopping pod.</summary>
public sealed class ShoppingAiService(
    EmbeddingService embeddings,
    LlmRunner llm,
    ShoppingQueries shoppingQueries,
    StorePilotDbContext db) : IShoppingAi
{
    public async Task<IReadOnlyList<ShopProductCard>> SearchAsync(string query, CancellationToken ct = default)
    {
        var hits = await embeddings.SearchAsync(query, [KnowledgeSourceType.Product], 8, ct);

        if (hits.Count == 0)
        {
            // Keyword fallback keeps search working when Ollama is down.
            var matches = await db.Products.AsNoTracking()
                .Where(p => p.Name.Contains(query) || p.Description.Contains(query) || p.Brand.Contains(query))
                .Select(p => p.Id)
                .Take(8)
                .ToListAsync(ct);
            var fallbackCards = await shoppingQueries.ToCardsAsync(matches, ct);
            return fallbackCards.Values.Select(c => c with { Reason = "keyword match" }).ToList();
        }

        var cards = await shoppingQueries.ToCardsAsync(hits.Select(h => h.SourceId).ToList(), ct);
        return hits
            .Where(h => cards.ContainsKey(h.SourceId))
            .Select(h => cards[h.SourceId] with { Score = Math.Round(h.Score, 3), Reason = "semantic match" })
            .ToList();
    }

    public async Task<AssistantAnswer> AskAsync(string question, CancellationToken ct = default)
    {
        var hits = await embeddings.SearchAsync(question, null, 6, ct);

        var context = new StringBuilder();
        foreach (var hit in hits)
            context.AppendLine($"[{hit.SourceType}: {hit.Title}]\n{hit.Content}\n");
        if (hits.Count == 0)
            context.AppendLine("(no retrieval context available)");

        string answer = await llm.RunAsync(
            "shopping.assistant", AssistantPrompt.Version, AssistantPrompt.System,
            $"Context:\n{context}\nCustomer question: {question}", ct);

        var sources = hits
            .Select(h => new AssistantSource(h.Title, h.SourceType.ToString()))
            .DistinctBy(s => s.Title)
            .ToList();
        return new AssistantAnswer(answer, sources);
    }

    public async Task<string> CompareAsync(IReadOnlyList<Guid> productIds, CancellationToken ct = default)
    {
        var cards = await shoppingQueries.ToCardsAsync(productIds.ToList(), ct);
        var described = await db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(ct);
        if (described.Count < 2)
            return "Pick at least two products to compare.";

        var context = new StringBuilder();
        foreach (var p in described)
        {
            var stock = cards.GetValueOrDefault(p.Id);
            context.AppendLine(
                $"- {p.Name} ({p.Brand}, {p.Sku}): {p.Description} Price ${p.Price:F2}. "
                + (stock is { InStock: true } ? $"In stock ({stock.CurrentStock})." : "Currently out of stock."));
        }

        return await llm.RunAsync(
            "shopping.compare", ComparePrompt.Version, ComparePrompt.System,
            $"Products:\n{context}", ct);
    }
}

/// <summary>
/// Inventory Copilot — archetype C, grounded variant: the deterministic analysis
/// snapshot is the only source of numbers; the model only phrases the answer.
/// </summary>
public sealed class InventoryCopilotService(
    InventorySnapshotCache cache,
    InventoryAnalysisService analysis,
    StorePilotDbContext db,
    LlmRunner llm) : IInventoryCopilot
{
    public async Task<CopilotAnswer> AskAsync(string question, CancellationToken ct = default)
    {
        if (cache.Health is null)
            await analysis.RunAsync(ct);

        var snapshot = new StringBuilder();
        var health = cache.Health!;
        snapshot.AppendLine(
            $"HEALTH: score {health.HealthScore}/100 · {health.TotalProducts} products · stock value ${health.TotalStockValue:F0} · "
            + $"low stock {health.LowStockCount} · dead {health.DeadCount} (value ${health.DeadValue:F0}) · "
            + $"overstock {health.OverstockCount} (value ${health.OverstockValue:F0}) · fast movers {health.FastCount} · slow movers {health.SlowCount}");

        void Section(string title, IEnumerable<InventoryProductRow> rows, Func<InventoryProductRow, string> line)
        {
            var list = rows.ToList();
            if (list.Count == 0) return;
            snapshot.AppendLine($"\n{title}:");
            foreach (var row in list.Take(10))
                snapshot.AppendLine("  " + line(row));
        }

        Section("LOW STOCK (at/below reorder point)",
            cache.Rows.Where(r => r.Classification == StockClass.LowStock),
            r => $"{r.Name} [{r.Sku}] {r.Brand}: {r.CurrentStock} on hand, reorder point {r.ReorderPoint}"
               + (r.DaysUntilStockout is { } d ? $", ~{d} days to stockout" : ""));
        Section("DEAD STOCK (no sales in 60d)",
            cache.Rows.Where(r => r.Classification == StockClass.Dead),
            r => $"{r.Name} [{r.Sku}] {r.Brand}: {r.CurrentStock} units, ${r.StockValue:F0} locked");
        Section("OVERSTOCK",
            cache.Rows.Where(r => r.Classification == StockClass.Overstock),
            r => $"{r.Name} [{r.Sku}] {r.Brand}: {r.CurrentStock} units vs {r.DailyRate:F1}/day demand");
        Section("TOP MOVERS",
            cache.Rows.Where(r => r.Classification == StockClass.Fast).OrderByDescending(r => r.DailyRate),
            r => $"{r.Name} [{r.Sku}] {r.Brand}: {r.DailyRate:F1}/day, 30d forecast {r.Forecast30}, {r.CurrentStock} on hand");

        var suggestions = await db.ReorderSuggestions.AsNoTracking()
            .OrderBy(s => s.OrderByDate).Take(10).ToListAsync(ct);
        if (suggestions.Count > 0)
        {
            var names = cache.Rows.ToDictionary(r => r.ProductId, r => $"{r.Name} [{r.Sku}]");
            snapshot.AppendLine("\nREORDER SUGGESTIONS:");
            foreach (var s in suggestions)
                snapshot.AppendLine(
                    $"  {names.GetValueOrDefault(s.ProductId, s.ProductId.ToString())}: order {s.Quantity} by {s.OrderByDate:yyyy-MM-dd} ({s.Rationale})");
        }

        string answer = await llm.RunAsync(
            "inventory.copilot", CopilotPrompt.Version, CopilotPrompt.System,
            $"Inventory snapshot (generated {health.GeneratedAt:u}):\n{snapshot}\nManager question: {question}", ct);
        return new CopilotAnswer(answer);
    }
}
