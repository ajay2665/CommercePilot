using StorePilot.Application.Abstractions;
using FastEndpoints;

namespace StorePilot.Api.Features.Inventory.Commands.AcknowledgeAlert;

public sealed class AcknowledgeAlertEndpoint(IInventoryQueries queries) : EndpointWithoutRequest
{
    public const string Route = "/api/inventory/alerts/{id}/ack";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!await queries.AcknowledgeAlertAsync(Route<Guid>("id"), ct))
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(cancellation: ct);
    }
}
