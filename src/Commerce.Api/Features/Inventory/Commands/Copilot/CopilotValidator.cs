using FastEndpoints;
using FluentValidation;

namespace Commerce.Api.Features.Inventory.Commands.Copilot;

public sealed class CopilotValidator : Validator<CopilotRequest>
{
    public CopilotValidator() => RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
}
