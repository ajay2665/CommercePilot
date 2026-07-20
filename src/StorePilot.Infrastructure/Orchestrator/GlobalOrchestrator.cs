using StorePilot.Application.Abstractions;
using StorePilot.Application.Ai.Prompts;
using StorePilot.Infrastructure.Ai;
using Microsoft.Extensions.Logging;
using System.Text;

namespace StorePilot.Infrastructure.Orchestrator;

/// <summary>
/// The AI command center pipeline: classify intent → run the matching agents
/// (data gathering only, sequential — they share the scoped DbContext) →
/// one synthesis LLM call over the combined context. Two LLM round-trips per
/// message total. Degrades to raw data blocks when the model is down.
/// </summary>
public sealed class GlobalOrchestrator(
    IIntentClassifier classifier,
    IEnumerable<IBusinessAgent> agents,
    ConversationMemory memory,
    LlmRunner llm,
    ILogger<GlobalOrchestrator> logger) : IGlobalOrchestrator
{
    private const string CapabilitiesContext = """
        CAPABILITIES: The assistant answers questions about this storepilot business using live data:
        - sales: revenue, orders, growth, top products, top customers ("What is this month's sales report?")
        - inventory: stock health, low/dead stock, forecasts, reorder suggestions ("What should we reorder?")
        - support: ticket volumes, triage pipeline, escalations ("Any escalated tickets?")
        - shopping: product search, trending products, comparisons ("What's trending right now?")
        - overall business performance ("How is the business doing?")
        """;

    public async Task<BrainChatResponse> HandleAsync(
        string message, Guid? conversationId, string? currentPage, CancellationToken ct = default)
    {
        Guid conversation = conversationId ?? Guid.NewGuid();
        var turns = memory.GetTurns(conversation);
        var recent = turns.TakeLast(4).Select(t => $"{t.Role}: {t.Content}").ToList();

        var intent = await classifier.ClassifyAsync(message, recent, ct);

        var selected = agents.Where(a => intent.Domains.Contains(a.Domain)).ToList();
        var query = new AgentQuery(message, intent.Days, currentPage);

        var contexts = new List<AgentContext>();
        foreach (var agent in selected)
        {
            try
            {
                contexts.Add(await agent.GatherAsync(query, ct));
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "{Domain} agent failed; continuing without it", agent.Domain);
            }
        }

        string dataBlocks = contexts.Count == 0
            ? CapabilitiesContext
            : string.Join("\n", contexts.Select(c => c.Context));

        var prompt = new StringBuilder();
        if (turns.Count > 0)
        {
            prompt.AppendLine("Conversation so far:");
            foreach (var t in turns.TakeLast(6))
                prompt.AppendLine($"{t.Role}: {t.Content}");
            prompt.AppendLine();
        }
        prompt.AppendLine($"Data blocks:\n{dataBlocks}");
        prompt.AppendLine($"\nUser question: {message}");

        string reply = await llm.RunAsync(
            "brain.chat", SynthesisPrompt.Version, SynthesisPrompt.System, prompt.ToString(), ct);

        if (reply == LlmRunner.UnavailableMessage && contexts.Count > 0)
            reply = $"{LlmRunner.UnavailableMessage}\n\nHere is the raw data for your question:\n\n{dataBlocks}";

        memory.Append(conversation, message, reply);

        var primaryContext = contexts.FirstOrDefault(c => c.Domain == intent.Primary);
        return new BrainChatResponse(
            Reply: reply,
            ConversationId: conversation,
            Intent: intent.Primary,
            Domains: intent.Domains,
            Sources: contexts.SelectMany(c => c.Sources).DistinctBy(s => s.Title).ToList(),
            Actions: contexts.SelectMany(c => c.Actions).DistinctBy(a => a.Route).ToList(),
            Chart: primaryContext?.Chart ?? contexts.FirstOrDefault(c => c.Chart is not null)?.Chart);
    }
}
