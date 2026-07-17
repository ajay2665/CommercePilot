using Commerce.Application.Features.Support;
using Commerce.Domain.Entities;
using FastEndpoints;

namespace Commerce.Api.Features.Platform;

public sealed class ListNotificationsRequest
{
    public bool UnacknowledgedOnly { get; set; }
    public int Take { get; set; } = 50;
}

public sealed class ListNotificationsEndpoint
    : Endpoint<ListNotificationsRequest, IReadOnlyList<Notification>>
{
    public override void Configure()
    {
        Get("/api/notifications");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListNotificationsRequest req, CancellationToken ct)
        => Response = await new ListNotificationsQuery(req.UnacknowledgedOnly, req.Take).ExecuteAsync(ct);
}

public sealed class AcknowledgeNotificationEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/notifications/{id}/ack");
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
