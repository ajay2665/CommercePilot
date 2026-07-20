namespace StorePilot.Domain.Entities;

public class KnowledgeArticle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public KnowledgeSourceType SourceType { get; set; } = KnowledgeSourceType.KbArticle;
    public string Brand { get; set; } = "*";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Polymorphic chunk store for all embedded content. Vector is 768 float32s
/// (raw little-endian bytes); similarity is computed in-process (IVectorSearch).
/// </summary>
public class EmbeddingChunk
{
    public long Id { get; set; }
    public KnowledgeSourceType SourceType { get; set; }
    public Guid SourceId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = "";
    public string MetadataJson { get; set; } = "{}";
    public byte[] Vector { get; set; } = [];
}

/// <summary>One row per LLM call — the observability trail behind /api/admin/ai-usage.</summary>
public class AiInteraction
{
    public long Id { get; set; }
    public string Feature { get; set; } = "";
    public string PromptVersion { get; set; } = "";
    public string Model { get; set; } = "";
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public long LatencyMs { get; set; }
    public decimal CostUsd { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Feedback { get; set; }
    public bool HallucinationFlag { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>In-app notification — the local INotifier target (Slack is the prod toggle).</summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Channel { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
    public Guid? TicketId { get; set; }
    public bool Acknowledged { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
