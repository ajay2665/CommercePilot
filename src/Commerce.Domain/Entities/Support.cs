namespace Commerce.Domain.Entities;

/// <summary>
/// A support ticket. Classification fields stay null while Status is Queued;
/// the TriageWorker fills them and moves the ticket to Triaged or Escalated.
/// </summary>
public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public string Sender { get; set; } = "";

    public string? Brand { get; set; }
    public TicketCategory? Category { get; set; }
    public TicketUrgency? Urgency { get; set; }
    public TicketSentiment? Sentiment { get; set; }
    public double? Confidence { get; set; }
    public string? Summary { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Queued;
    public string? AssignedTeam { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? TriagedAt { get; set; }
}

/// <summary>
/// Routing config: which team (and notification channel) handles a brand+category.
/// Brand "*" is the wildcard fallback; brand-specific rules take precedence.
/// </summary>
public class RoutingRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Brand { get; set; } = "*";
    public TicketCategory Category { get; set; }
    public TicketUrgency UrgencyThreshold { get; set; } = TicketUrgency.Low;
    public string TargetTeam { get; set; } = "";
    public string? NotifyChannel { get; set; }
}
