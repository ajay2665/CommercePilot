namespace StorePilot.Domain.Entities;

public enum ShopEventType { View, Cart, Purchase }

public class CustomerEvent
{
    public long Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public ShopEventType EventType { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Materialised nightly by the co-occurrence job (decision 7: SQL math, not LLM).</summary>
public class Recommendation
{
    public long Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public double Score { get; set; }
    public string Source { get; set; } = "co-purchase"; // co-purchase | trending
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class AbandonedCart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string ProductIdsJson { get; set; } = "[]";
    public DateTimeOffset LastActiveAt { get; set; } = DateTimeOffset.UtcNow;
    public bool RecoveryEmailSent { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
