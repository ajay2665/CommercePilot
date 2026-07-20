using StorePilot.Domain;

namespace StorePilot.Api.Features.Support.Queries.ListTickets;

public sealed class ListTicketsRequest
{
    public string? Team { get; set; }
    public TicketStatus? Status { get; set; }
    public TicketUrgency? Urgency { get; set; }
    public string? Brand { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
