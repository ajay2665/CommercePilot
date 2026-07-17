using Commerce.Application.Abstractions;
using Commerce.Domain.Entities;
using FastEndpoints;

namespace Commerce.Api.Features.Inventory.Queries.GetAlerts;

public sealed class GetAlertsEndpoint(IInventoryQueries queries)
    : Endpoint<InventoryAlertsRequest, IReadOnlyList<InventoryAlert>>
{
    public const string Route = "/api/inventory/alerts";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(InventoryAlertsRequest req, CancellationToken ct)
        => Response = await queries.GetAlertsAsync(req.UnacknowledgedOnly, ct);
}
