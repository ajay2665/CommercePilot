namespace StorePilot.Api.Features.Shopping.Commands.Compare;

public sealed class CompareRequest
{
    public List<Guid> ProductIds { get; set; } = [];
}
