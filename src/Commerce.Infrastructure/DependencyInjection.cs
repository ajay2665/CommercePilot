using Commerce.Application.Abstractions;
using Commerce.Application.Events;
using Commerce.Application.Features.Support;
using Commerce.Application.Options;
using Commerce.Infrastructure.Ai;
using Commerce.Infrastructure.Messaging;
using Commerce.Infrastructure.Notifications;
using Commerce.Infrastructure.Persistence;
using Commerce.Infrastructure.Support;
using Commerce.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Commerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // ── Persistence ─────────────────────────────────────────────────────
        services.AddDbContext<CommerceDbContext>(o =>
            o.UseSqlServer(config.GetConnectionString("Default")));
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IRoutingRuleRepository, RoutingRuleRepository>();
        services.AddScoped<INotificationStore, NotificationStore>();

        // ── Options ─────────────────────────────────────────────────────────
        services.Configure<LlmOptions>(config.GetSection("Llm"));
        services.Configure<SupportOptions>(config.GetSection("Support"));

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

        return services;
    }
}
