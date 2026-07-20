using FastEndpoints;
using FluentValidation;

namespace StorePilot.Api.Features.Shopping.Commands.Compare;

public sealed class CompareValidator : Validator<CompareRequest>
{
    public CompareValidator() => RuleFor(x => x.ProductIds).NotEmpty();
}
