namespace StorePilot.Application.Options;

public sealed class InventoryOptions
{
    public int AnalysisIntervalHours { get; set; } = 6;
    public int DeadStockDays { get; set; } = 60;
    public int OverstockDaysOfDemand { get; set; } = 90;
    public int FastMoverTopPercent { get; set; } = 25;
}

public sealed class ShoppingOptions
{
    public int AbandonedCartHours { get; set; } = 4;
    public int RecommendationsPerCustomer { get; set; } = 6;
    public int TrendingWindowDays { get; set; } = 30;
}
