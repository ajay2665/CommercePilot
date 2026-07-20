using StorePilot.Domain;
using StorePilot.Domain.Entities;

namespace StorePilot.Application.Features.Support;

/// <summary>
/// Pure routing precedence: exact (brand, category) → wildcard ("*", category)
/// → brand catch-all (brand, Other) → global catch-all ("*", Other) → null.
/// </summary>
public static class RoutingResolver
{
    public static RoutingRule? Resolve(IReadOnlyList<RoutingRule> rules, string brand, TicketCategory category)
        => Find(rules, brand, category)
        ?? Find(rules, "*", category)
        ?? Find(rules, brand, TicketCategory.Other)
        ?? Find(rules, "*", TicketCategory.Other);

    private static RoutingRule? Find(IReadOnlyList<RoutingRule> rules, string brand, TicketCategory category)
        => rules.FirstOrDefault(r =>
            r.Category == category &&
            string.Equals(r.Brand, brand, StringComparison.OrdinalIgnoreCase));
}
