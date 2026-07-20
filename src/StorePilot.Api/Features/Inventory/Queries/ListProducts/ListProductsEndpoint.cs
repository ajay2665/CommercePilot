using StorePilot.Application.Abstractions;
using FastEndpoints;

namespace StorePilot.Api.Features.Inventory.Queries.ListProducts;

public sealed class ListProductsEndpoint(IInventoryQueries queries)
    : EndpointWithoutRequest<IReadOnlyList<InventoryProductRow>>
{
    public const string Route = "/api/inventory/products";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
        => Response = await queries.ListProductsAsync(ct);
}
