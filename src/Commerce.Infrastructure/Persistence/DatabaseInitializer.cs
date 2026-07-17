using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Applies pending migrations, then seeds the demo dataset when the DB is empty.</summary>
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
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
    }
}
