using Commerce.Application.Abstractions;
using Commerce.Application.Features.Support;
using Commerce.Domain.Entities;
using Commerce.Shared;
using FastEndpoints;

namespace Commerce.Api.Features.Support.Queries.ListTickets;

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
