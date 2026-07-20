using StorePilot.Application.Abstractions;
using FastEndpoints;

namespace StorePilot.Api.Features.Shopping.Queries.GetCustomers;

public sealed class GetCustomersEndpoint(IShoppingQueries queries)
    : EndpointWithoutRequest<IReadOnlyList<CustomerLite>>
{
    public const string Route = "/api/shopping/customers";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
        => Response = await queries.GetCustomersAsync(ct);
}
