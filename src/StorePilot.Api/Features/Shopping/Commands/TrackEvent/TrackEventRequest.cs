using StorePilot.Domain.Entities;

namespace StorePilot.Api.Features.Shopping.Commands.TrackEvent;

public sealed class TrackEventRequest
{
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public ShopEventType EventType { get; set; }
}
