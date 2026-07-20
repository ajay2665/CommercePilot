using Commerce.Application.Abstractions;
using FastEndpoints;

namespace Commerce.Api.Features.Dashboard;

/// <summary>
/// Common query range for all dashboard endpoints. Defaults to the last 30
/// days when from/to are omitted.
/// </summary>
public sealed class DashboardRangeRequest
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public string? Granularity { get; set; }
    public int Take { get; set; } = 10;

    public (DateTimeOffset From, DateTimeOffset To) Range()
    {
        DateTimeOffset to = To ?? DateTimeOffset.UtcNow;
        DateTimeOffset from = From ?? to.AddDays(-30);
        return from < to ? (from, to) : (to.AddDays(-30), to);
    }
}

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
