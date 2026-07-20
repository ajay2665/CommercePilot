using FastEndpoints;
using FluentValidation;

namespace Commerce.Api.Features.Shopping.Commands.TrackEvent;

public sealed class TrackEventValidator : Validator<TrackEventRequest>
{
    public TrackEventValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
