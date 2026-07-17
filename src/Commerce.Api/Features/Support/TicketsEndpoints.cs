using Commerce.Application.Abstractions;
using Commerce.Application.Features.Support;
using Commerce.Domain;
using Commerce.Domain.Entities;
using Commerce.Shared;
using FastEndpoints;

namespace Commerce.Api.Features.Support;

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

public sealed class ListTicketsEndpoint : Endpoint<ListTicketsRequest, PagedResult<Ticket>>
{
    public override void Configure()
    {
        Get("/api/support/tickets");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListTicketsRequest req, CancellationToken ct)
    {
        var filter = new TicketFilter(req.Team, req.Status, req.Urgency, req.Brand, req.Search, req.Page, req.PageSize);
        Response = await new ListTicketsQuery(filter).ExecuteAsync(ct);
    }
}

public sealed class GetTicketEndpoint : EndpointWithoutRequest<Ticket>
{
    public override void Configure()
    {
        Get("/api/support/tickets/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        Guid id = Route<Guid>("id");
        Ticket? ticket = await new GetTicketQuery(id).ExecuteAsync(ct);
        if (ticket is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        Response = ticket;
    }
}

public sealed class ListRoutingRulesEndpoint : EndpointWithoutRequest<IReadOnlyList<RoutingRule>>
{
    public override void Configure()
    {
        Get("/api/support/routing-rules");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
        => Response = await new ListRoutingRulesQuery().ExecuteAsync(ct);
}
