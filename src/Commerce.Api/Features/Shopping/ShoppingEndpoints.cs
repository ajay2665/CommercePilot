using Commerce.Application.Abstractions;
using Commerce.Domain.Entities;
using FastEndpoints;
using FluentValidation;

namespace Commerce.Api.Features.Shopping;

public sealed class SearchRequest
{
    public string Q { get; set; } = "";
}

public sealed class SemanticSearchEndpoint(IShoppingAi ai)
    : Endpoint<SearchRequest, IReadOnlyList<ShopProductCard>>
{
    public override void Configure()
    {
        Get("/api/shopping/search");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SearchRequest req, CancellationToken ct)
        => Response = string.IsNullOrWhiteSpace(req.Q) ? [] : await ai.SearchAsync(req.Q.Trim(), ct);
}

public sealed class RecommendationsRequest
{
    public Guid CustomerId { get; set; }
}

public sealed class RecommendationsEndpoint(IShoppingQueries queries)
    : Endpoint<RecommendationsRequest, IReadOnlyList<ShopProductCard>>
{
    public override void Configure()
    {
        Get("/api/shopping/recommendations");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RecommendationsRequest req, CancellationToken ct)
        => Response = await queries.GetRecommendationsAsync(req.CustomerId, ct);
}

public sealed class TrendingEndpoint(IShoppingQueries queries)
    : EndpointWithoutRequest<IReadOnlyList<ShopProductCard>>
{
    public override void Configure()
    {
        Get("/api/shopping/trending");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
        => Response = await queries.GetTrendingAsync(ct);
}

public sealed class TrackEventRequest
{
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public ShopEventType EventType { get; set; }
}

public sealed class TrackEventValidator : Validator<TrackEventRequest>
{
    public TrackEventValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

public sealed class TrackEventEndpoint(IShoppingQueries queries) : Endpoint<TrackEventRequest>
{
    public override void Configure()
    {
        Post("/api/shopping/events");
        AllowAnonymous();
    }

    public override async Task HandleAsync(TrackEventRequest req, CancellationToken ct)
    {
        await queries.TrackEventAsync(req.CustomerId, req.ProductId, req.EventType, ct);
        await Send.OkAsync(cancellation: ct);
    }
}

public sealed class CustomersEndpoint(IShoppingQueries queries)
    : EndpointWithoutRequest<IReadOnlyList<CustomerLite>>
{
    public override void Configure()
    {
        Get("/api/shopping/customers");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
        => Response = await queries.GetCustomersAsync(ct);
}

public sealed class AssistantRequest
{
    public string Question { get; set; } = "";
}

public sealed class AssistantValidator : Validator<AssistantRequest>
{
    public AssistantValidator() => RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
}

public sealed class AssistantEndpoint(IShoppingAi ai) : Endpoint<AssistantRequest, AssistantAnswer>
{
    public override void Configure()
    {
        Post("/api/shopping/assistant");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AssistantRequest req, CancellationToken ct)
        => Response = await ai.AskAsync(req.Question.Trim(), ct);
}

public sealed class CompareRequest
{
    public List<Guid> ProductIds { get; set; } = [];
}

public sealed class CompareValidator : Validator<CompareRequest>
{
    public CompareValidator() => RuleFor(x => x.ProductIds).NotEmpty();
}

public sealed record CompareResponse(string Comparison);

public sealed class CompareEndpoint(IShoppingAi ai) : Endpoint<CompareRequest, CompareResponse>
{
    public override void Configure()
    {
        Post("/api/shopping/compare");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CompareRequest req, CancellationToken ct)
        => Response = new CompareResponse(await ai.CompareAsync(req.ProductIds, ct));
}
