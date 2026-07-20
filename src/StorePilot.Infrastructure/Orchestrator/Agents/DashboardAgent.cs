using StorePilot.Application.Abstractions;

namespace StorePilot.Infrastructure.Orchestrator.Agents;

/// <summary>
/// Cross-module business overview: one compact snapshot line per domain from
/// the knowledge base, for "how is the business doing" style questions.
/// </summary>
public sealed class DashboardAgent(IGlobalKnowledgeBase knowledgeBase) : IBusinessAgent
{
    public string Domain => "dashboard";

    public async Task<AgentContext> GatherAsync(AgentQuery query, CancellationToken ct = default)
    {
        string context = await knowledgeBase.BuildContextAsync(["dashboard"], query.Days, ct);

        return new AgentContext(Domain,
            $"BUSINESS OVERVIEW (last {query.Days} days):\n{context}",
            Sources:
            [
                new ChatSource("Orders", "database"),
                new ChatSource("Inventory analysis snapshot", "analysis"),
                new ChatSource("Tickets", "database"),
            ],
            Actions: [new ActionLink("Open dashboard", "/")],
            Chart: null);
    }
}
