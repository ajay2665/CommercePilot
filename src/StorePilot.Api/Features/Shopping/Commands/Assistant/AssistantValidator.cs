using FastEndpoints;
using FluentValidation;

namespace StorePilot.Api.Features.Shopping.Commands.Assistant;

public sealed class AssistantValidator : Validator<AssistantRequest>
{
    public AssistantValidator() => RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
}
