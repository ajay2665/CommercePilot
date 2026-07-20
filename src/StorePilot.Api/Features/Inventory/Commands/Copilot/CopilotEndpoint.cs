using StorePilot.Application.Abstractions;
using FastEndpoints;

namespace StorePilot.Api.Features.Inventory.Commands.Copilot;

public sealed class CopilotEndpoint(IInventoryCopilot copilot)
    : Endpoint<CopilotRequest, CopilotAnswer>
{
    public const string Route = "/api/inventory/copilot";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CopilotRequest req, CancellationToken ct)
        => Response = await copilot.AskAsync(req.Question, ct);
}
