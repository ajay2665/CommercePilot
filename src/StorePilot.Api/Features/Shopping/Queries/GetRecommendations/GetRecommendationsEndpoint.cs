using StorePilot.Application.Abstractions;
using FastEndpoints;

namespace StorePilot.Api.Features.Shopping.Queries.GetRecommendations;

public sealed class GetRecommendationsEndpoint(IShoppingQueries queries)
    : Endpoint<RecommendationsRequest, IReadOnlyList<ShopProductCard>>
{
    public const string Route = "/api/shopping/recommendations";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(RecommendationsRequest req, CancellationToken ct)
        => Response = await queries.GetRecommendationsAsync(req.CustomerId, ct);
}
