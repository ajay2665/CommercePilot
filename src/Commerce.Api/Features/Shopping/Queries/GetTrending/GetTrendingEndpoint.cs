using Commerce.Application.Abstractions;
using FastEndpoints;

namespace Commerce.Api.Features.Shopping.Queries.GetTrending;

public sealed class GetTrendingEndpoint(IShoppingQueries queries)
    : EndpointWithoutRequest<IReadOnlyList<ShopProductCard>>
{
    public const string Route = "/api/shopping/trending";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
        => Response = await queries.GetTrendingAsync(ct);
}
