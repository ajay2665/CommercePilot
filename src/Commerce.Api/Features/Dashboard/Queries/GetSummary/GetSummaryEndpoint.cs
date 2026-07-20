using Commerce.Api.Features.Dashboard.Queries.Shared;
using Commerce.Application.Abstractions;
using FastEndpoints;

namespace Commerce.Api.Features.Dashboard.Queries.GetSummary;

public sealed class GetSummaryEndpoint(IDashboardQueries queries)
    : Endpoint<DashboardRangeRequest, DashboardSummary>
{
    public const string Route = "/api/dashboard/summary";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(DashboardRangeRequest req, CancellationToken ct)
    {
        var (from, to) = req.Range();
        Response = await queries.GetSummaryAsync(from, to, ct);
    }
}
