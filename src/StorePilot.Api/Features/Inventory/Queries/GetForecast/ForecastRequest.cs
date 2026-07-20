namespace StorePilot.Api.Features.Inventory.Queries.GetForecast;

public sealed class ForecastRequest
{
    public Guid ProductId { get; set; }
    public int Horizon { get; set; } = 30;
}
