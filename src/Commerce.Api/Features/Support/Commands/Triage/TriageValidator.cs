using FastEndpoints;
using FluentValidation;

namespace Commerce.Api.Features.Support.Commands.Triage;

public sealed class TriageValidator : Validator<TriageRequest>
{
    public TriageValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Body).NotEmpty();
        RuleFor(x => x.Sender).NotEmpty().MaximumLength(256);
    }
}
