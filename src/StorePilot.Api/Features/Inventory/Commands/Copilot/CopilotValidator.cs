using FastEndpoints;
using FluentValidation;

namespace StorePilot.Api.Features.Inventory.Commands.Copilot;

public sealed class CopilotValidator : Validator<CopilotRequest>
{
    public CopilotValidator() => RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
}
