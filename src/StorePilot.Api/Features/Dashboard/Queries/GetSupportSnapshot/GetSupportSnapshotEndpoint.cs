using StorePilot.Api.Features.Dashboard.Queries.Shared;
using StorePilot.Application.Abstractions;
using FastEndpoints;

namespace StorePilot.Api.Features.Dashboard.Queries.GetSupportSnapshot;

public sealed class GetSupportSnapshotEndpoint(IDashboardQueries queries)
    : Endpoint<DashboardRangeRequest, SupportSnapshot>
{
    public const string Route = "/api/dashboard/support-snapshot";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(DashboardRangeRequest req, CancellationToken ct)
    {
        var (from, to) = req.Range();
        Response = await queries.GetSupportSnapshotAsync(from, to, ct);
    }
}
