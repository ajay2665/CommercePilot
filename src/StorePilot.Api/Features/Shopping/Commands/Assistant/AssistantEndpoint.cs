using StorePilot.Application.Abstractions;
using FastEndpoints;

namespace StorePilot.Api.Features.Shopping.Commands.Assistant;

public sealed class AssistantEndpoint(IShoppingAi ai) : Endpoint<AssistantRequest, AssistantAnswer>
{
    public const string Route = "/api/shopping/assistant";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(AssistantRequest req, CancellationToken ct)
        => Response = await ai.AskAsync(req.Question.Trim(), ct);
}
