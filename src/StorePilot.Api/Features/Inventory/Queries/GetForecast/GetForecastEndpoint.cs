using StorePilot.Application.Abstractions;
using FastEndpoints;

namespace StorePilot.Api.Features.Inventory.Queries.GetForecast;

public sealed class GetForecastEndpoint(IInventoryQueries queries)
    : Endpoint<ForecastRequest, ForecastSeries>
{
    public const string Route = "/api/inventory/forecast";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(ForecastRequest req, CancellationToken ct)
    {
        var series = await queries.GetForecastAsync(req.ProductId, req.Horizon, ct);
        if (series is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        Response = series;
    }
}
