using System.Text;
using Commerce.Application.Abstractions;

namespace Commerce.Infrastructure.Orchestrator.Agents;

/// <summary>
/// Product discovery: semantic search hits for the question plus what is
/// currently trending. Reuses the shopping pod's search (with its keyword
/// fallback) — no extra LLM calls here.
/// </summary>
public sealed class ShoppingAgent(
    IShoppingAi shoppingAi,
    IShoppingQueries shoppingQueries) : IBusinessAgent
{
    public string Domain => "shopping";

    public async Task<AgentContext> GatherAsync(AgentQuery query, CancellationToken ct = default)
    {
        var matches = await shoppingAi.SearchAsync(query.Question, ct);
        var trending = await shoppingQueries.GetTrendingAsync(ct);

        var context = new StringBuilder();
        if (matches.Count > 0)
        {
            context.AppendLine("PRODUCTS MATCHING THE QUESTION:\n");
            context.AppendLine("| Product | SKU | Brand | Price | Stock |");
            context.AppendLine("|---|---|---|---|---|");
            foreach (var p in matches.Take(6))
                context.AppendLine(
                    $"| {p.Name} | {p.Sku} | {p.Brand} | ${p.Price:F2} | {(p.InStock ? p.CurrentStock.ToString() : "Out")} |");
            context.AppendLine();
        }

        if (trending.Count > 0)
        {
            context.AppendLine("TRENDING PRODUCTS (by recent customer events):\n");
            context.AppendLine("| Product | SKU | Brand | Price | Reason |");
            context.AppendLine("|---|---|---|---|---|");
            foreach (var p in trending.Take(6))
                context.AppendLine(
                    $"| {p.Name} | {p.Sku} | {p.Brand} | ${p.Price:F2} | {p.Reason} |");
            context.AppendLine();
        }

        if (context.Length == 0)
            context.AppendLine("No matching or trending products found.");

        var sources = matches.Take(6).Select(p => new ChatSource(p.Name, "product")).ToList();
        if (sources.Count == 0)
            sources = [new ChatSource("Product catalog", "database")];

        return new AgentContext(Domain, context.ToString(),
            Sources: sources,
            Actions: [new ActionLink("Open Shopping AI", "/shopping")],
            Chart: null);
    }
}
