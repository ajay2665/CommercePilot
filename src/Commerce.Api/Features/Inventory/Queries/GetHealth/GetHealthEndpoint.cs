using Commerce.Application.Abstractions;
using FastEndpoints;

namespace Commerce.Api.Features.Inventory.Queries.GetHealth;

public sealed class GetHealthEndpoint(IInventoryQueries queries)
    : EndpointWithoutRequest<HealthSummary>
{
    public const string Route = "/api/inventory/health";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
        => Response = await queries.GetHealthAsync(ct);
}
