namespace StorePilot.Api.Features.Dashboard.Queries.Shared;

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
