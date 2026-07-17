using Commerce.Application.Abstractions;
using Commerce.Domain.Entities;
using FastEndpoints;
using FluentValidation;

namespace Commerce.Api.Features.Inventory;

public sealed class InventoryProductsEndpoint(IInventoryQueries queries)
    : EndpointWithoutRequest<IReadOnlyList<InventoryProductRow>>
{
    public override void Configure()
    {
        Get("/api/inventory/products");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
        => Response = await queries.ListProductsAsync(ct);
}

public sealed class InventoryHealthEndpoint(IInventoryQueries queries)
    : EndpointWithoutRequest<HealthSummary>
{
    public override void Configure()
    {
        Get("/api/inventory/health");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
        => Response = await queries.GetHealthAsync(ct);
}

public sealed class ForecastRequest
{
    public Guid ProductId { get; set; }
    public int Horizon { get; set; } = 30;
}

public sealed class InventoryForecastEndpoint(IInventoryQueries queries)
    : Endpoint<ForecastRequest, ForecastSeries>
{
    public override void Configure()
    {
        Get("/api/inventory/forecast");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ForecastRequest req, CancellationToken ct)
    {
        var series = await queries.GetForecastAsync(req.ProductId, req.Horizon, ct);
        if (series is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        Response = series;
    }
}

public sealed class InventoryAlertsRequest
{
    public bool UnacknowledgedOnly { get; set; }
}

public sealed class InventoryAlertsEndpoint(IInventoryQueries queries)
    : Endpoint<InventoryAlertsRequest, IReadOnlyList<InventoryAlert>>
{
    public override void Configure()
    {
        Get("/api/inventory/alerts");
        AllowAnonymous();
    }

    public override async Task HandleAsync(InventoryAlertsRequest req, CancellationToken ct)
        => Response = await queries.GetAlertsAsync(req.UnacknowledgedOnly, ct);
}

public sealed class AcknowledgeAlertEndpoint(IInventoryQueries queries) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/inventory/alerts/{id}/ack");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!await queries.AcknowledgeAlertAsync(Route<Guid>("id"), ct))
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(cancellation: ct);
    }
}

public sealed class ReorderSuggestionsEndpoint(IInventoryQueries queries)
    : EndpointWithoutRequest<IReadOnlyList<ReorderSuggestion>>
{
    public override void Configure()
    {
        Get("/api/inventory/reorder-suggestions");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
        => Response = await queries.GetReorderSuggestionsAsync(ct);
}

public sealed class CopilotRequest
{
    public string Question { get; set; } = "";
}

public sealed class CopilotValidator : Validator<CopilotRequest>
{
    public CopilotValidator() => RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
}

public sealed class InventoryCopilotEndpoint(IInventoryCopilot copilot)
    : Endpoint<CopilotRequest, CopilotAnswer>
{
    public override void Configure()
    {
        Post("/api/inventory/copilot");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CopilotRequest req, CancellationToken ct)
        => Response = await copilot.AskAsync(req.Question, ct);
}
