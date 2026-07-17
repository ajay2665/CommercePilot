using Commerce.Application.Abstractions;
using Commerce.Domain.Entities;
using Commerce.Shared;
using FastEndpoints;

namespace Commerce.Application.Features.Support;

public sealed record ListTicketsQuery(TicketFilter Filter) : ICommand<PagedResult<Ticket>>;

public sealed class ListTicketsHandler(ITicketRepository tickets)
    : ICommandHandler<ListTicketsQuery, PagedResult<Ticket>>
{
    public Task<PagedResult<Ticket>> ExecuteAsync(ListTicketsQuery query, CancellationToken ct)
        => tickets.ListAsync(query.Filter, ct);
}

public sealed record GetTicketQuery(Guid TicketId) : ICommand<Ticket?>;

public sealed class GetTicketHandler(ITicketRepository tickets) : ICommandHandler<GetTicketQuery, Ticket?>
{
    public Task<Ticket?> ExecuteAsync(GetTicketQuery query, CancellationToken ct)
        => tickets.GetAsync(query.TicketId, ct);
}

public sealed record ListRoutingRulesQuery : ICommand<IReadOnlyList<RoutingRule>>;

public sealed class ListRoutingRulesHandler(IRoutingRuleRepository rules)
    : ICommandHandler<ListRoutingRulesQuery, IReadOnlyList<RoutingRule>>
{
    public Task<IReadOnlyList<RoutingRule>> ExecuteAsync(ListRoutingRulesQuery query, CancellationToken ct)
        => rules.GetAllAsync(ct);
}

public sealed record ListNotificationsQuery(bool UnacknowledgedOnly = false, int Take = 50)
    : ICommand<IReadOnlyList<Notification>>;

public sealed class ListNotificationsHandler(INotificationStore store)
    : ICommandHandler<ListNotificationsQuery, IReadOnlyList<Notification>>
{
    public Task<IReadOnlyList<Notification>> ExecuteAsync(ListNotificationsQuery query, CancellationToken ct)
        => store.ListAsync(query.UnacknowledgedOnly, query.Take, ct);
}

public sealed record AcknowledgeNotificationCommand(Guid NotificationId) : ICommand<bool>;

public sealed class AcknowledgeNotificationHandler(INotificationStore store)
    : ICommandHandler<AcknowledgeNotificationCommand, bool>
{
    public Task<bool> ExecuteAsync(AcknowledgeNotificationCommand command, CancellationToken ct)
        => store.AcknowledgeAsync(command.NotificationId, ct);
}
