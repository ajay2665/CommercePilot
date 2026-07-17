namespace Commerce.Domain.Entities;

public enum StockClass { Healthy, Fast, Slow, Dead, Overstock, LowStock }

public enum AlertType { LowStock, StockoutRisk, DeadStock, Overstock }

public class StockMovement
{
    public long Id { get; set; }
    public Guid ProductId { get; set; }
    public int Delta { get; set; }              // negative = outbound (sale), positive = restock
    public string Reason { get; set; } = "";    // sale | restock | correction
    public string Source { get; set; } = "";    // seed | order | manual
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Nightly snapshot per product + horizon (7/30/90 days) from the moving-average engine.</summary>
public class DemandForecast
{
    public long Id { get; set; }
    public Guid ProductId { get; set; }
    public int HorizonDays { get; set; }
    public double DailyRate { get; set; }
    public int PredictedUnits { get; set; }
    public double Confidence { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class InventoryAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public AlertType Type { get; set; }
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Warning;
    public string Message { get; set; } = "";
    public bool Acknowledged { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Smart reorder engine output: when, how much, from whom (spec: Inventory AI #2).</summary>
public class ReorderSuggestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Guid? SupplierId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset OrderByDate { get; set; }
    public string Rationale { get; set; } = "";
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}
