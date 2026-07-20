using Commerce.Application.Abstractions;
using Commerce.Application.Ai.Prompts;
using Commerce.Infrastructure.Ai;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Commerce.Infrastructure.Orchestrator;

/// <summary>
/// Keyword-first intent classification: unambiguous messages route without any
/// LLM round-trip (local CPU inference is minutes-slow, and the synthesis call
/// already costs one). The LLM classifier only runs when keywords say nothing,
/// e.g. multi-turn follow-ups like "and what about last week?".
/// </summary>
public sealed class IntentClassifier(LlmRunner llm, ILogger<IntentClassifier> logger) : IIntentClassifier
{
    private static readonly string[] KnownDomains =
        ["sales", "inventory", "support", "shopping", "dashboard", "general"];

    public async Task<IntentResult> ClassifyAsync(
        string message, IReadOnlyList<string> recentTurns, CancellationToken ct = default)
    {
        var byKeywords = KeywordClassify(message);
        if (byKeywords is not null)
            return byKeywords;

        string history = recentTurns.Count == 0
            ? ""
            : "Recent conversation:\n" + string.Join("\n", recentTurns) + "\n\n";

        string raw = await llm.RunAsync(
            "brain.intent", IntentPrompt.Version, IntentPrompt.System,
            $"{history}Message: {message}", ct);

        var parsed = TryParse(raw);
        if (parsed is not null)
            return parsed;

        logger.LogWarning("Intent classification defaulted to general for: {Message}", message);
        return new IntentResult(["general"], "general", DaysFrom(message));
    }

    private static IntentResult? TryParse(string raw)
    {
        int start = raw.IndexOf('{');
        int end = raw.LastIndexOf('}');
        if (start < 0 || end <= start) return null;

        try
        {
            using var doc = JsonDocument.Parse(raw[start..(end + 1)]);
            var root = doc.RootElement;

            var domains = root.TryGetProperty("domains", out var d) && d.ValueKind == JsonValueKind.Array
                ? d.EnumerateArray()
                    .Select(x => x.GetString()?.ToLowerInvariant() ?? "")
                    .Where(x => KnownDomains.Contains(x))
                    .Distinct()
                    .Take(3)
                    .ToList()
                : [];
            if (domains.Count == 0) return null;

            string primary = root.TryGetProperty("primary", out var p)
                             && p.GetString()?.ToLowerInvariant() is { } pr && domains.Contains(pr)
                ? pr
                : domains[0];
            int days = root.TryGetProperty("days", out var dd) && dd.TryGetInt32(out int val)
                ? Math.Clamp(val, 1, 365)
                : 30;

            return new IntentResult(domains, primary, days);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Null when no domain keyword matches — the caller then asks the LLM.</summary>
    private static IntentResult? KeywordClassify(string message)
    {
        string m = message.ToLowerInvariant();
        var domains = new List<string>();

        if (ContainsAny(m, "overall", "performance", "how is the business", "how's the business",
                "summary", "kpi", "dashboard", "report"))
            domains.Add("dashboard");
        if (ContainsAny(m, "revenue", "sales", "sold", "selling", "order", "customer", "profit",
                "growth", "aov", "spend"))
            domains.Add("sales");
        if (ContainsAny(m, "stock", "inventory", "reorder", "restock", "forecast", "warehouse",
                "stockout", "running low", "running out", "supplier"))
            domains.Add("inventory");
        if (ContainsAny(m, "ticket", "support", "escalat", "triage", "complaint", "refund", "csat"))
            domains.Add("support");
        if (ContainsAny(m, "product", "trending", "recommend", "compare", "search", "shopping",
            "catalog", "best seller", "bestseller"))
            domains.Add("shopping");
        if (ContainsAny(m, "hello", "hi ", "hey", "help", "what can you"))
            domains.Add("general");

        // "report" alone often pairs with a domain ("sales report") — prefer the
        // specific domain over dashboard when both matched.
        if (domains.Count > 1 && domains[0] == "dashboard" && !m.Contains("overall") && !m.Contains("business"))
            domains.RemoveAt(0);

        return domains.Count == 0
            ? null
            : new IntentResult(domains.Take(3).ToList(), domains[0], DaysFrom(m));
    }

    private static int DaysFrom(string message)
    {
        string m = message.ToLowerInvariant();
        return m.Contains("today") ? 1
            : m.Contains("week") ? 7
            : m.Contains("quarter") ? 90
            : m.Contains("year") ? 365
            : 30;
    }

    private static bool ContainsAny(string text, params string[] terms)
        => terms.Any(text.Contains);
}
