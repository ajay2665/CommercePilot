using Commerce.Api.Features.Dashboard.Queries.Shared;
using Commerce.Application.Abstractions;
using FastEndpoints;

namespace Commerce.Api.Features.Dashboard.Queries.GetRecentActivity;

public sealed class GetRecentActivityEndpoint(IDashboardQueries queries)
    : Endpoint<DashboardRangeRequest, IReadOnlyList<ActivityItem>>
{
    public const string Route = "/api/dashboard/recent-activity";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(DashboardRangeRequest req, CancellationToken ct)
        => Response = await queries.GetRecentActivityAsync(Math.Clamp(req.Take, 1, 100), ct);
}
