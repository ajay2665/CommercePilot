using Commerce.Domain;

namespace Commerce.Application.Abstractions;

public sealed record NotificationMessage(
    string Channel,
    string Title,
    string Message,
    NotificationSeverity Severity = NotificationSeverity.Info,
    Guid? TicketId = null);

/// <summary>Local impl: console + in-app notifications table. Slack webhook is the prod toggle.</summary>
public interface INotifier
{
    Task NotifyAsync(NotificationMessage message, CancellationToken ct = default);
}
