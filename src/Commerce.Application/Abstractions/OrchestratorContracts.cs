namespace Commerce.Application.Abstractions;

// ── Global orchestrator / AI command center (Phase 4) ────────────────────────

public sealed record ChatSource(string Title, string Kind);

public sealed record ActionLink(string Label, string Route);

/// <summary>Structured data the frontend renders as an inline chart.</summary>
public sealed record ChatChartSeries(string Label, IReadOnlyList<decimal> Data);

public sealed record ChatChart(string Type, IReadOnlyList<string> Labels, IReadOnlyList<ChatChartSeries> Datasets);

public sealed record BrainChatResponse(
    string Reply, Guid ConversationId, string Intent,
    IReadOnlyList<string> Domains,
    IReadOnlyList<ChatSource> Sources,
    IReadOnlyList<ActionLink> Actions,
    ChatChart? Chart);

/// <summary>Classified intent for a chat message. Days is the lookback window.</summary>
public sealed record IntentResult(IReadOnlyList<string> Domains, string Primary, int Days);

public sealed record AgentQuery(string Question, int Days, string? CurrentPage);

/// <summary>
/// What an agent contributes to a chat turn: a factual context block for the
/// synthesis prompt (agents never call the LLM themselves) plus UI extras.
/// </summary>
public sealed record AgentContext(
    string Domain, string Context,
    IReadOnlyList<ChatSource> Sources,
    IReadOnlyList<ActionLink> Actions,
    ChatChart? Chart);

public interface IBusinessAgent
{
    /// <summary>Intent domain this agent serves: sales | inventory | support | shopping | dashboard.</summary>
    string Domain { get; }

    Task<AgentContext> GatherAsync(AgentQuery query, CancellationToken ct = default);
}

public interface IIntentClassifier
{
    Task<IntentResult> ClassifyAsync(string message, IReadOnlyList<string> recentTurns, CancellationToken ct = default);
}

public interface IGlobalOrchestrator
{
    Task<BrainChatResponse> HandleAsync(string message, Guid? conversationId, string? currentPage, CancellationToken ct = default);
}

/// <summary>
/// Semantic access layer over the existing data — compact cross-module context
/// snapshots for prompts plus vector search over the embedded catalog/KB.
/// </summary>
public interface IGlobalKnowledgeBase
{
    Task<string> BuildContextAsync(IReadOnlyList<string> domains, int days, CancellationToken ct = default);
    Task<IReadOnlyList<ChatSource>> SemanticSearchAsync(string query, int topK, CancellationToken ct = default);
}
