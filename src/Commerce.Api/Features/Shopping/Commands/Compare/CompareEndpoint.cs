using Commerce.Application.Abstractions;
using FastEndpoints;

namespace Commerce.Api.Features.Shopping.Commands.Compare;

public sealed class CompareEndpoint(IShoppingAi ai) : Endpoint<CompareRequest, CompareResponse>
{
    public const string Route = "/api/shopping/compare";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CompareRequest req, CancellationToken ct)
        => Response = new CompareResponse(await ai.CompareAsync(req.ProductIds, ct));
}
