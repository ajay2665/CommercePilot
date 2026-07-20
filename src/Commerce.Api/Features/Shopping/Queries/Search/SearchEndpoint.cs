using Commerce.Application.Abstractions;
using Commerce.Domain.Entities;
using FastEndpoints;

namespace Commerce.Api.Features.Shopping.Queries.Search;

public sealed class SearchEndpoint(IShoppingAi ai)
    : Endpoint<SearchRequest, IReadOnlyList<ShopProductCard>>
{
    public const string Route = "/api/shopping/search";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(SearchRequest req, CancellationToken ct)
        => Response = string.IsNullOrWhiteSpace(req.Q) ? [] : await ai.SearchAsync(req.Q.Trim(), ct);
}
