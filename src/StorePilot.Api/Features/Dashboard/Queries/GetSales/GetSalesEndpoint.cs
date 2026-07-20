using StorePilot.Api.Features.Dashboard.Queries.Shared;
using StorePilot.Application.Abstractions;
using FastEndpoints;

namespace StorePilot.Api.Features.Dashboard.Queries.GetSales;

public sealed class GetSalesEndpoint(IDashboardQueries queries)
    : Endpoint<DashboardRangeRequest, SalesTrend>
{
    public const string Route = "/api/dashboard/sales";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(DashboardRangeRequest req, CancellationToken ct)
    {
        var (from, to) = req.Range();
        Response = await queries.GetSalesTrendAsync(from, to, req.Granularity ?? "day", ct);
    }
}
