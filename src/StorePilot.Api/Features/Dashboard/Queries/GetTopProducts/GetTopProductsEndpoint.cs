using StorePilot.Api.Features.Dashboard.Queries.Shared;
using StorePilot.Application.Abstractions;
using FastEndpoints;

namespace StorePilot.Api.Features.Dashboard.Queries.GetTopProducts;

public sealed class GetTopProductsEndpoint(IDashboardQueries queries)
    : Endpoint<DashboardRangeRequest, IReadOnlyList<TopProduct>>
{
    public const string Route = "/api/dashboard/top-products";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(DashboardRangeRequest req, CancellationToken ct)
    {
        var (from, to) = req.Range();
        Response = await queries.GetTopProductsAsync(from, to, Math.Clamp(req.Take, 1, 50), ct);
    }
}
