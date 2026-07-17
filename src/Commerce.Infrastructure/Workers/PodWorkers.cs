using System.Text.Json;
using Commerce.Application.Abstractions;
using Commerce.Application.Options;
using Commerce.Domain;
using Commerce.Infrastructure.Ai;
using Commerce.Infrastructure.Inventory;
using Commerce.Infrastructure.Persistence;
using Commerce.Infrastructure.Shopping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Commerce.Infrastructure.Workers;

/// <summary>Runs the deterministic inventory analysis at startup and on an interval.</summary>
public sealed class InventoryAnalysisWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<InventoryOptions> options,
    ILogger<InventoryAnalysisWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); // let migrate/seed finish
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<InventoryAnalysisService>().RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Inventory analysis run failed");
            }
            await Task.Delay(TimeSpan.FromHours(options.Value.AnalysisIntervalHours), stoppingToken);
        }
    }
}

/// <summary>Nightly co-occurrence recommendation rebuild (plus one at startup).</summary>
public sealed class RecommendationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ShoppingOptions> options,
    ILogger<RecommendationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
                await RecommendationBuilder.RebuildAsync(db, options.Value.RecommendationsPerCustomer, logger, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Recommendation rebuild failed");
            }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}

/// <summary>
/// Abandoned-cart recovery: composes the recovery email and, in local mode,
/// delivers it as a notification + console line (SMTP/Gmail is the prod toggle).
/// </summary>
public sealed class CartRecoveryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ShoppingOptions> options,
    ILogger<CartRecoveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Cart recovery pass failed");
            }
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        var notifier = scope.ServiceProvider.GetRequiredService<INotifier>();

        DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddHours(-options.Value.AbandonedCartHours);
        var due = await db.AbandonedCarts
            .Where(c => !c.RecoveryEmailSent && c.LastActiveAt < cutoff)
            .ToListAsync(ct);
        if (due.Count == 0) return;

        var customers = (await db.Customers.AsNoTracking().ToListAsync(ct)).ToDictionary(c => c.Id);
        var products = (await db.Products.AsNoTracking().ToListAsync(ct)).ToDictionary(p => p.Id);

        foreach (var cart in due)
        {
            var ids = JsonSerializer.Deserialize<List<Guid>>(cart.ProductIdsJson) ?? [];
            var names = ids.Where(products.ContainsKey).Select(id => products[id].Name).ToList();
            decimal total = ids.Where(products.ContainsKey).Sum(id => products[id].Price);
            string email = customers.GetValueOrDefault(cart.CustomerId)?.Email ?? "unknown@example.com";

            await notifier.NotifyAsync(new NotificationMessage(
                Channel: "Cart Recovery",
                Title: $"Recovery email → {email}",
                Message: $"\"You left {string.Join(", ", names)} (${total:F2}) in your cart — complete your order!\"",
                Severity: NotificationSeverity.Info), ct);

            cart.RecoveryEmailSent = true;
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Cart recovery: {Count} recovery email(s) sent (local mode: logged as notifications)", due.Count);
    }
}

/// <summary>Warms the embedding cache shortly after boot so the first search is instant.</summary>
public sealed class EmbeddingWarmupWorker(EmbeddingService embeddings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        await embeddings.EnsureReadyAsync(stoppingToken);
    }
}
