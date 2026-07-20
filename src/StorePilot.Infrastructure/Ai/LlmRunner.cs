using StorePilot.Domain.Entities;
using StorePilot.Infrastructure.Persistence;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace StorePilot.Infrastructure.Ai;

/// <summary>
/// Shared chat runner for the copilot/assistant/compare features: retry, latency
/// measurement, and the ai_interactions observability row on every attempt.
/// Never throws — a dead model degrades to an apologetic answer, not a 500.
/// </summary>
public sealed class LlmRunner(
    IChatClient chat,
    StorePilotDbContext db,
    IOptions<LlmOptions> llmOptions,
    ILogger<LlmRunner> logger)
{
    private const int MaxAttempts = 2;

    public const string UnavailableMessage =
        "The local AI model is not responding — is Ollama running? (ollama serve, model "
        + "per Llm:ChatModel in appsettings.json)";

    public async Task<string> RunAsync(
        string feature, string promptVersion, string systemPrompt, string userPrompt,
        CancellationToken ct = default)
    {
        List<ChatMessage> messages =
        [
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt),
        ];
        var options = new ChatOptions();

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ChatResponse response = await chat.GetResponseAsync(messages, options, ct);
                stopwatch.Stop();
                string text = response.Text?.Trim() ?? "";

                await LogAsync(feature, promptVersion, response, stopwatch.ElapsedMilliseconds,
                    success: text.Length > 0, error: text.Length > 0 ? null : "empty response", ct);
                if (text.Length > 0)
                    return text;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException or System.ClientModel.ClientResultException
                                       && !ct.IsCancellationRequested)
            {
                stopwatch.Stop();
                await LogAsync(feature, promptVersion, null, stopwatch.ElapsedMilliseconds,
                    success: false, error: ex.Message, ct);
                logger.LogWarning("{Feature} attempt {Attempt}/{Max} failed: {Error}",
                    feature, attempt, MaxAttempts, ex.Message);
                if (attempt < MaxAttempts)
                    await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }

        return UnavailableMessage;
    }

    private async Task LogAsync(
        string feature, string promptVersion, ChatResponse? response, long latencyMs,
        bool success, string? error, CancellationToken ct)
    {
        db.AiInteractions.Add(new AiInteraction
        {
            Feature = feature,
            PromptVersion = promptVersion,
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
