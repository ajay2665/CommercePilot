using Commerce.Application.Features.Support;
using Commerce.Domain.Entities;
using FastEndpoints;

namespace Commerce.Api.Features.Support.Queries.GetTicket;

public sealed class GetTicketEndpoint : EndpointWithoutRequest<Ticket>
{
    public const string Route = "/api/support/tickets/{id}";

    public override void Configure()
    {
        Get(Route);
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
