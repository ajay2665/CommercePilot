using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StorePilot.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Applies pending migrations, then seeds the demo dataset when the DB is empty.</summary>
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StorePilotDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseInitializer");

        await db.Database.MigrateAsync(ct);

        if (!await db.Products.AnyAsync(ct))
        {
            logger.LogInformation("Empty database detected — seeding demo dataset…");
            await DemoSeeder.SeedAsync(db, ct);
            logger.LogInformation(
                "Seeded: {Products} products, {Orders} orders, {Rules} routing rules, {Articles} KB articles",
                await db.Products.CountAsync(ct), await db.Orders.CountAsync(ct),
                await db.RoutingRules.CountAsync(ct), await db.KnowledgeArticles.CountAsync(ct));
        }

        // Phase 2/3 backfill — also upgrades a Phase-1 database in place.
        if (!await db.StockMovements.AnyAsync(ct))
        {
            logger.LogInformation("Backfilling pod data (stock movements, customer events, carts)…");
            await DemoSeeder.SeedPodDataAsync(db, ct);
            logger.LogInformation(
                "Pod data: {Movements} movements, {Events} customer events, {Carts} abandoned carts",
                await db.StockMovements.CountAsync(ct), await db.CustomerEvents.CountAsync(ct),
                await db.AbandonedCarts.CountAsync(ct));
        }
    }
}
