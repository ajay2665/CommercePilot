using StorePilot.Application.Features.Support;
using FastEndpoints;

namespace StorePilot.Api.Features.Platform.Commands.AcknowledgeNotification;

public sealed class AcknowledgeNotificationEndpoint : EndpointWithoutRequest
{
    public const string Route = "/api/notifications/{id}/ack";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        Guid id = Route<Guid>("id");
        bool found = await new AcknowledgeNotificationCommand(id).ExecuteAsync(ct);
        if (!found)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(cancellation: ct);
    }
}
