using Commerce.Application.Abstractions;
using Commerce.Domain.Entities;
using FastEndpoints;

namespace Commerce.Api.Features.Inventory.Queries.GetReorderSuggestions;

public sealed class GetReorderSuggestionsEndpoint(IInventoryQueries queries)
    : EndpointWithoutRequest<IReadOnlyList<ReorderSuggestion>>
{
    public const string Route = "/api/inventory/reorder-suggestions";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
        => Response = await queries.GetReorderSuggestionsAsync(ct);
}
