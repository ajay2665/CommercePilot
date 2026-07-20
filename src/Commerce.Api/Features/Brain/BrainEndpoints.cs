using Commerce.Application.Abstractions;
using FastEndpoints;
using FluentValidation;

namespace Commerce.Api.Features.Brain;

public sealed class BrainChatRequest
{
    public string Message { get; set; } = "";
    public Guid? ConversationId { get; set; }
    public string? CurrentPage { get; set; }
}

public sealed class BrainChatValidator : Validator<BrainChatRequest>
{
    public BrainChatValidator() => RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
}

public sealed class BrainChatEndpoint(IGlobalOrchestrator orchestrator)
    : Endpoint<BrainChatRequest, BrainChatResponse>
{
    public const string Route = "/api/brain/chat";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
    }

    public override async Task HandleAsync(BrainChatRequest req, CancellationToken ct)
        => Response = await orchestrator.HandleAsync(req.Message, req.ConversationId, req.CurrentPage, ct);
}
