using StorePilot.Application.Abstractions;
using StorePilot.Application.Events;
using StorePilot.Application.Features.Support;
using StorePilot.Application.Options;
using StorePilot.Infrastructure.Ai;
using StorePilot.Infrastructure.Dashboard;
using StorePilot.Infrastructure.Inventory;
using StorePilot.Infrastructure.Messaging;
using StorePilot.Infrastructure.Notifications;
using StorePilot.Infrastructure.Orchestrator;
using StorePilot.Infrastructure.Orchestrator.Agents;
using StorePilot.Infrastructure.Persistence;
using StorePilot.Infrastructure.Shopping;
using StorePilot.Infrastructure.Support;
using StorePilot.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StorePilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // ── Persistence ─────────────────────────────────────────────────────
        services.AddDbContext<StorePilotDbContext>(o =>
            o.UseSqlServer(config.GetConnectionString("Default")));
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IRoutingRuleRepository, RoutingRuleRepository>();
        services.AddScoped<INotificationStore, NotificationStore>();

        // ── Options ─────────────────────────────────────────────────────────
        services.Configure<LlmOptions>(config.GetSection("Llm"));
        services.Configure<SupportOptions>(config.GetSection("Support"));
        services.Configure<InventoryOptions>(config.GetSection("Inventory"));
        services.Configure<ShoppingOptions>(config.GetSection("Shopping"));

        // ── Messaging: in-process bus + intake queue (decision 3) ───────────
        services.AddSingleton<InProcessEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<InProcessEventBus>());
        services.AddSingleton<IWorkQueue<TriageWorkItem>, ChannelWorkQueue<TriageWorkItem>>();
        services.AddHostedService<EventDispatcherService>();

        // ── Notifications (local mode; Slack is the prod toggle) ────────────
        services.AddScoped<INotifier, LocalNotifier>();
        services.AddScoped<IEventHandler<TicketCreated>, TicketCreatedNotificationHandler>();
        services.AddScoped<IEventHandler<TicketEscalated>, TicketEscalatedNotificationHandler>();

        // ── AI (decision 5: provider behind IChatClient) ────────────────────
        services.AddSingleton<IChatClient>(sp =>
            LlmClientFactory.CreateChatClient(sp.GetRequiredService<IOptions<LlmOptions>>().Value));
        services.AddScoped<ITriageClassifier, TriageClassifier>();

        // ── Support pipeline ────────────────────────────────────────────────
        services.AddScoped<TriageProcessor>();
        services.AddSingleton<TriageStatus>();
        services.AddHostedService<TriageWorker>();

        // ── Inventory pod (Phase 2) ─────────────────────────────────────────
        services.AddSingleton<InventorySnapshotCache>();
        services.AddScoped<InventoryAnalysisService>();
        services.AddScoped<IInventoryQueries, InventoryQueries>();
        services.AddScoped<IEventHandler<StockLow>, StockLowNotificationHandler>();
        services.AddScoped<IInventoryCopilot, InventoryCopilotService>();
        services.AddHostedService<InventoryAnalysisWorker>();

        // ── Shopping pod (Phase 3) ──────────────────────────────────────────
        services.AddScoped<ShoppingQueries>();
        services.AddScoped<IShoppingQueries>(sp => sp.GetRequiredService<ShoppingQueries>());
        services.AddScoped<IShoppingAi, ShoppingAiService>();
        services.AddHostedService<RecommendationWorker>();
        services.AddHostedService<CartRecoveryWorker>();

        // ── Executive dashboard (Phase 4) ───────────────────────────────────
        services.AddScoped<IDashboardQueries, DashboardQueries>();

        // ── Global orchestrator: AI command center (Phase 4) ────────────────
        services.AddSingleton<ConversationMemory>();
        services.AddScoped<IIntentClassifier, IntentClassifier>();
        services.AddScoped<IGlobalKnowledgeBase, GlobalKnowledgeBase>();
        services.AddScoped<IBusinessAgent, SalesAgent>();
        services.AddScoped<IBusinessAgent, InventoryAgent>();
        services.AddScoped<IBusinessAgent, SupportAgent>();
        services.AddScoped<IBusinessAgent, ShoppingAgent>();
        services.AddScoped<IBusinessAgent, DashboardAgent>();
        services.AddScoped<IGlobalOrchestrator, GlobalOrchestrator>();

        // ── Shared AI: chat runner + embeddings/vector search ───────────────
        services.AddScoped<LlmRunner>();
        services.AddSingleton<EmbeddingService>();
        services.AddHostedService<EmbeddingWarmupWorker>();
        services.AddHostedService<ModelKeepWarmWorker>();

        return services;
    }
}
