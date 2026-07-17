namespace Commerce.Api.Features.Platform.Queries.ListNotifications;

public sealed class ListNotificationsRequest
{
    public bool UnacknowledgedOnly { get; set; }
    public int Take { get; set; } = 50;
}
