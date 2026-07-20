using StorePilot.Application.Abstractions;
using StorePilot.Domain.Entities;
using StorePilot.Shared;
using Microsoft.EntityFrameworkCore;

namespace StorePilot.Infrastructure.Persistence;

public sealed class TicketRepository(StorePilotDbContext db) : ITicketRepository
{
    public async Task AddAsync(Ticket ticket, CancellationToken ct = default)
        => await db.Tickets.AddAsync(ticket, ct);

    public Task<Ticket?> GetAsync(Guid id, CancellationToken ct = default)
        => db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResult<Ticket>> ListAsync(TicketFilter filter, CancellationToken ct = default)
    {
        IQueryable<Ticket> query = db.Tickets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Team))
            query = query.Where(t => t.AssignedTeam == filter.Team);
        if (filter.Status is { } status)
            query = query.Where(t => t.Status == status);
        if (filter.Urgency is { } urgency)
            query = query.Where(t => t.Urgency == urgency);
        if (!string.IsNullOrWhiteSpace(filter.Brand))
            query = query.Where(t => t.Brand == filter.Brand);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(t => t.Subject.Contains(filter.Search) || t.Sender.Contains(filter.Search));

        int total = await query.CountAsync(ct);
        int page = Math.Max(1, filter.Page);
        int size = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedResult<Ticket>(items, total, page, size);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}

public sealed class RoutingRuleRepository(StorePilotDbContext db) : IRoutingRuleRepository
{
    public async Task<IReadOnlyList<RoutingRule>> GetAllAsync(CancellationToken ct = default)
        => await db.RoutingRules.AsNoTracking().ToListAsync(ct);
}

public sealed class NotificationStore(StorePilotDbContext db) : INotificationStore
{
    public async Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        await db.Notifications.AddAsync(notification, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Notification>> ListAsync(bool unacknowledgedOnly, int take, CancellationToken ct = default)
    {
        IQueryable<Notification> query = db.Notifications.AsNoTracking();
        if (unacknowledgedOnly)
            query = query.Where(n => !n.Acknowledged);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(ct);
    }

    public async Task<bool> AcknowledgeAsync(Guid id, CancellationToken ct = default)
    {
        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (notification is null)
            return false;

        notification.Acknowledged = true;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
