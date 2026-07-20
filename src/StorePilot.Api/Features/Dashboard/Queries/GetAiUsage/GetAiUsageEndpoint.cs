using StorePilot.Api.Features.Dashboard.Queries.Shared;
using StorePilot.Application.Abstractions;
using FastEndpoints;

namespace StorePilot.Api.Features.Dashboard.Queries.GetAiUsage;

public sealed class GetAiUsageEndpoint(IDashboardQueries queries)
    : Endpoint<DashboardRangeRequest, AiUsageStats>
{
    public const string Route = "/api/dashboard/ai-usage";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(DashboardRangeRequest req, CancellationToken ct)
    {
        var (from, to) = req.Range();
        Response = await queries.GetAiUsageAsync(from, to, ct);
    }
}
