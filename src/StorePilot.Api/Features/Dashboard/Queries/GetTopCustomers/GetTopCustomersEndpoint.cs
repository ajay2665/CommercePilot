using StorePilot.Api.Features.Dashboard.Queries.Shared;
using StorePilot.Application.Abstractions;
using FastEndpoints;

namespace StorePilot.Api.Features.Dashboard.Queries.GetTopCustomers;

public sealed class GetTopCustomersEndpoint(IDashboardQueries queries)
    : Endpoint<DashboardRangeRequest, IReadOnlyList<TopCustomer>>
{
    public const string Route = "/api/dashboard/top-customers";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(DashboardRangeRequest req, CancellationToken ct)
    {
        var (from, to) = req.Range();
        Response = await queries.GetTopCustomersAsync(from, to, Math.Clamp(req.Take, 1, 50), ct);
    }
}
