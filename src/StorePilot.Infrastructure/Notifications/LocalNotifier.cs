using StorePilot.Application.Abstractions;
using StorePilot.Domain;
using StorePilot.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace StorePilot.Infrastructure.Notifications;

/// <summary>
/// Local-first INotifier (decision 10): writes an in-app notification row the
/// dashboard panel shows, plus a console line. SlackNotifier is the prod toggle.
/// </summary>
public sealed class LocalNotifier(INotificationStore store, ILogger<LocalNotifier> logger) : INotifier
{
    public async Task NotifyAsync(NotificationMessage message, CancellationToken ct = default)
    {
        await store.AddAsync(new Notification
        {
            Channel = message.Channel,
            Title = message.Title,
            Message = message.Message,
            Severity = message.Severity,
            TicketId = message.TicketId,
        }, ct);

        var level = message.Severity switch
        {
            NotificationSeverity.Critical => LogLevel.Warning,
            NotificationSeverity.Warning => LogLevel.Warning,
            _ => LogLevel.Information,
        };
        logger.Log(level, "[#{Channel}] {Title} — {Message}", message.Channel, message.Title, message.Message);
    }
}
