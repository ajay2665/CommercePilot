using StorePilot.Application.Abstractions;
using FastEndpoints;

namespace StorePilot.Api.Features.Shopping.Commands.TrackEvent;

public sealed class TrackEventEndpoint(IShoppingQueries queries) : Endpoint<TrackEventRequest>
{
    public const string Route = "/api/shopping/events";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(TrackEventRequest req, CancellationToken ct)
    {
        await queries.TrackEventAsync(req.CustomerId, req.ProductId, req.EventType, ct);
        await Send.OkAsync(cancellation: ct);
    }
}
