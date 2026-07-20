using FastEndpoints;
using FluentValidation;

namespace StorePilot.Api.Features.Shopping.Commands.TrackEvent;

public sealed class TrackEventValidator : Validator<TrackEventRequest>
{
    public TrackEventValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
