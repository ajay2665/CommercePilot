using Commerce.Domain;

namespace Commerce.Application.Abstractions;

public sealed record TriageInput(string Subject, string Body, string Sender);

public sealed record TicketClassification(
    string Brand,
    TicketCategory Category,
    TicketUrgency Urgency,
    TicketSentiment Sentiment,
    double Confidence,
    string Summary);

/// <summary>LLM-backed structured classification. Implementation lives in Infrastructure.</summary>
public interface ITriageClassifier
{
    Task<TicketClassification> ClassifyAsync(TriageInput input, CancellationToken ct = default);
}
