using StorePilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace StorePilot.Infrastructure.Persistence;

public sealed class StorePilotDbContext(DbContextOptions<StorePilotDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<InventoryLevel> InventoryLevels => Set<InventoryLevel>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();
    public DbSet<EmbeddingChunk> Embeddings => Set<EmbeddingChunk>();
    public DbSet<AiInteraction> AiInteractions => Set<AiInteraction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<RoutingRule> RoutingRules => Set<RoutingRule>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<DemandForecast> DemandForecasts => Set<DemandForecast>();
    public DbSet<InventoryAlert> InventoryAlerts => Set<InventoryAlert>();
    public DbSet<ReorderSuggestion> ReorderSuggestions => Set<ReorderSuggestion>();
    public DbSet<CustomerEvent> CustomerEvents => Set<CustomerEvent>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<AbandonedCart> AbandonedCarts => Set<AbandonedCart>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>(e =>
        {
            e.Property(p => p.Brand).HasMaxLength(64);
            e.Property(p => p.Sku).HasMaxLength(64);
            e.Property(p => p.Name).HasMaxLength(256);
            e.Property(p => p.Price).HasPrecision(18, 2);
            e.HasIndex(p => p.Sku).IsUnique();
            e.HasIndex(p => p.Brand);
        });

        b.Entity<Order>(e =>
        {
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(32);
            e.Property(o => o.Total).HasPrecision(18, 2);
            e.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
            e.HasIndex(o => o.CreatedAt);
        });

        b.Entity<OrderItem>(e => e.Property(i => i.UnitPrice).HasPrecision(18, 2));

        b.Entity<InventoryLevel>(e => e.HasIndex(i => i.ProductId));

        b.Entity<KnowledgeArticle>(e =>
        {
            e.Property(a => a.SourceType).HasConversion<string>().HasMaxLength(32);
            e.Property(a => a.Brand).HasMaxLength(64);
            e.Property(a => a.Title).HasMaxLength(256);
        });

        b.Entity<EmbeddingChunk>(e =>
        {
            e.ToTable("Embeddings");
            e.Property(c => c.SourceType).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(c => new { c.SourceType, c.SourceId });
        });

        b.Entity<AiInteraction>(e =>
        {
            e.Property(a => a.Feature).HasMaxLength(64);
            e.Property(a => a.PromptVersion).HasMaxLength(64);
            e.Property(a => a.Model).HasMaxLength(64);
            e.Property(a => a.CostUsd).HasPrecision(18, 6);
            e.HasIndex(a => a.CreatedAt);
        });

        b.Entity<Notification>(e =>
        {
            e.Property(n => n.Channel).HasMaxLength(128);
            e.Property(n => n.Title).HasMaxLength(256);
            e.Property(n => n.Severity).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(n => new { n.Acknowledged, n.CreatedAt });
        });

        b.Entity<Ticket>(e =>
        {
            e.Property(t => t.Subject).HasMaxLength(512);
            e.Property(t => t.Sender).HasMaxLength(256);
            e.Property(t => t.Brand).HasMaxLength(64);
            e.Property(t => t.AssignedTeam).HasMaxLength(128);
            e.Property(t => t.Category).HasConversion<string>().HasMaxLength(32);
            e.Property(t => t.Urgency).HasConversion<string>().HasMaxLength(32);
            e.Property(t => t.Sentiment).HasConversion<string>().HasMaxLength(32);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(t => t.Status);
            e.HasIndex(t => t.Brand);
            e.HasIndex(t => t.CreatedAt);
        });

        b.Entity<StockMovement>(e =>
        {
            e.Property(m => m.Reason).HasMaxLength(32);
            e.Property(m => m.Source).HasMaxLength(32);
            e.HasIndex(m => new { m.ProductId, m.Timestamp });
        });

        b.Entity<DemandForecast>(e => e.HasIndex(f => new { f.ProductId, f.HorizonDays }));

        b.Entity<InventoryAlert>(e =>
        {
            e.Property(a => a.Type).HasConversion<string>().HasMaxLength(32);
            e.Property(a => a.Severity).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(a => new { a.ProductId, a.Type, a.Acknowledged });
        });

        b.Entity<ReorderSuggestion>(e => e.HasIndex(r => r.ProductId));

        b.Entity<CustomerEvent>(e =>
        {
            e.Property(c => c.EventType).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(c => c.CustomerId);
            e.HasIndex(c => c.ProductId);
        });

        b.Entity<Recommendation>(e =>
        {
            e.Property(r => r.Source).HasMaxLength(32);
            e.HasIndex(r => r.CustomerId);
        });

        b.Entity<AbandonedCart>(e => e.HasIndex(c => new { c.RecoveryEmailSent, c.LastActiveAt }));

        b.Entity<RoutingRule>(e =>
        {
            e.Property(r => r.Brand).HasMaxLength(64);
            e.Property(r => r.Category).HasConversion<string>().HasMaxLength(32);
            e.Property(r => r.UrgencyThreshold).HasConversion<string>().HasMaxLength(32);
            e.Property(r => r.TargetTeam).HasMaxLength(128);
            e.Property(r => r.NotifyChannel).HasMaxLength(128);
            e.HasIndex(r => new { r.Brand, r.Category }).IsUnique();
        });
    }
}
