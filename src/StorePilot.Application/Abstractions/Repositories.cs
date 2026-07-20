using StorePilot.Domain;
using StorePilot.Domain.Entities;
using StorePilot.Shared;

namespace StorePilot.Application.Abstractions;

public sealed record TicketFilter(
    string? Team = null,
    TicketStatus? Status = null,
    TicketUrgency? Urgency = null,
    string? Brand = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public interface ITicketRepository
{
    Task AddAsync(Ticket ticket, CancellationToken ct = default);
    Task<Ticket?> GetAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Ticket>> ListAsync(TicketFilter filter, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IRoutingRuleRepository
{
    Task<IReadOnlyList<RoutingRule>> GetAllAsync(CancellationToken ct = default);
}

public interface INotificationStore
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> ListAsync(bool unacknowledgedOnly, int take, CancellationToken ct = default);
    Task<bool> AcknowledgeAsync(Guid id, CancellationToken ct = default);
}
