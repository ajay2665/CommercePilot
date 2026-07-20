using StorePilot.Application.Features.Support;
using StorePilot.Domain.Entities;
using FastEndpoints;

namespace StorePilot.Api.Features.Platform.Queries.ListNotifications;

public sealed class ListNotificationsEndpoint
    : Endpoint<ListNotificationsRequest, IReadOnlyList<Notification>>
{
    public const string Route = "/api/notifications";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListNotificationsRequest req, CancellationToken ct)
        => Response = await new ListNotificationsQuery(req.UnacknowledgedOnly, req.Take).ExecuteAsync(ct);
}
