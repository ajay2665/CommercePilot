using StorePilot.Application.Abstractions;
using StorePilot.Application.Features.Support;
using StorePilot.Domain.Entities;
using StorePilot.Shared;
using FastEndpoints;

namespace StorePilot.Api.Features.Support.Queries.ListTickets;

public sealed class ListTicketsEndpoint : Endpoint<ListTicketsRequest, PagedResult<Ticket>>
{
    public const string Route = "/api/support/tickets";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListTicketsRequest req, CancellationToken ct)
    {
        var filter = new TicketFilter(req.Team, req.Status, req.Urgency, req.Brand, req.Search, req.Page, req.PageSize);
        Response = await new ListTicketsQuery(filter).ExecuteAsync(ct);
    }
}
