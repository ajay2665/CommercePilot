using Commerce.Application.Abstractions;
using Commerce.Application.Ai.Prompts;
using Commerce.Application.Features.Support;
using Commerce.Application.Options;
using Commerce.Domain.Entities;
using Commerce.Infrastructure.Persistence;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Commerce.Infrastructure.Ai;

/// <summary>
/// Archetype A stage 1: structured-JSON classification on the local model.
/// Never throws — a dead LLM degrades to the fallback classification so the
/// pipeline (and the demo) keeps moving. Every attempt logs an ai_interactions row.
/// </summary>
public sealed class TriageClassifier(
    IChatClient chat,
    CommerceDbContext db,
    IOptions<SupportOptions> supportOptions,
    IOptions<LlmOptions> llmOptions,
    ILogger<TriageClassifier> logger) : ITriageClassifier
{
    private const string Feature = "support.triage";
    private const int MaxAttempts = 3;

    public async Task<TicketClassification> ClassifyAsync(TriageInput input, CancellationToken ct = default)
    {
        List<ChatMessage> messages =
        [
            new(ChatRole.System, TriagePrompt.BuildSystem(supportOptions.Value.Brands)),
            new(ChatRole.User, TriagePrompt.BuildUser(input.Subject, input.Sender, input.Body)),
        ];
        var options = new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.Json,
            Temperature = 0.1f,
        };

        int delaySeconds = 3;
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ChatResponse response = await chat.GetResponseAsync(messages, options, ct);
                stopwatch.Stop();
                string text = response.Text?.Trim() ?? "";

                bool parsed = ClassificationParser.TryParse(text, out TicketClassification classification);
                await LogInteractionAsync(response, stopwatch.ElapsedMilliseconds,
                    success: parsed, error: parsed ? null : "unparseable JSON", ct);

                if (parsed)
                    return classification;

                logger.LogWarning("Triage attempt {Attempt}: unparseable model output, retrying", attempt);
                // Corrective nudge for the retry — local models respond well to this.
                messages.Add(new ChatMessage(ChatRole.Assistant, text));
                messages.Add(new ChatMessage(ChatRole.User,
                    "That was not a single valid JSON object. Respond again with ONLY the JSON object."));
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException or System.ClientModel.ClientResultException
                                       && !ct.IsCancellationRequested)
            {
                stopwatch.Stop();
                await LogInteractionAsync(null, stopwatch.ElapsedMilliseconds, success: false, error: ex.Message, ct);
                logger.LogWarning("Triage attempt {Attempt}/{Max} failed: {Error} — retrying in {Delay}s",
                    attempt, MaxAttempts, ex.Message, delaySeconds);

                if (attempt < MaxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
                    delaySeconds *= 2;
                }
            }
        }

        logger.LogError("Triage failed after {Max} attempts — using fallback classification (is Ollama running?)", MaxAttempts);
        return ClassificationParser.Fallback(input.Subject);
    }

    private async Task LogInteractionAsync(
        ChatResponse? response, long latencyMs, bool success, string? error, CancellationToken ct)
    {
        db.AiInteractions.Add(new AiInteraction
        {
            Feature = Feature,
            PromptVersion = TriagePrompt.Version,
            Model = llmOptions.Value.ChatModel,
            InputTokens = (int)(response?.Usage?.InputTokenCount ?? 0),
            OutputTokens = (int)(response?.Usage?.OutputTokenCount ?? 0),
            LatencyMs = latencyMs,
            CostUsd = llmOptions.Value.CostFor(
                response?.Usage?.InputTokenCount ?? 0, response?.Usage?.OutputTokenCount ?? 0),
            Success = success,
            Error = error,
        });
        await db.SaveChangesAsync(ct);
    }
}
