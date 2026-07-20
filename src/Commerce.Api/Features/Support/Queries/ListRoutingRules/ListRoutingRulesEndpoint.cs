using Commerce.Application.Features.Support;
using Commerce.Domain.Entities;
using FastEndpoints;

namespace Commerce.Api.Features.Support.Queries.ListRoutingRules;

public sealed class ListRoutingRulesEndpoint : EndpointWithoutRequest<IReadOnlyList<RoutingRule>>
{
    public const string Route = "/api/support/routing-rules";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
        => Response = await new ListRoutingRulesQuery().ExecuteAsync(ct);
}
