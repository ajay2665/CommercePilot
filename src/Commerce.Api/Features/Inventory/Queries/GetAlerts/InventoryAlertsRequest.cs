namespace Commerce.Api.Features.Inventory.Queries.GetAlerts;

public sealed class InventoryAlertsRequest
{
    public bool UnacknowledgedOnly { get; set; }
}
