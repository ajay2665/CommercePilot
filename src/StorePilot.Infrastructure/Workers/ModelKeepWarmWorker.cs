using StorePilot.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StorePilot.Infrastructure.Workers;

/// <summary>
/// Keeps the local chat model resident by pinging it every few minutes —
/// Ollama unloads after ~5 idle minutes and a cold reload of an 8B model on
/// CPU costs the first user a minute-plus. Cloud providers don't need this,
/// so it only runs for the ollama provider (Llm:KeepWarm to disable).
/// </summary>
public sealed class ModelKeepWarmWorker(
    IChatClient chat,
    IOptions<LlmOptions> options,
    ILogger<ModelKeepWarmWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Ollama.KeepWarm || !options.Value.IsOllamaChat)
            return;

        var ping = new ChatOptions { MaxOutputTokens = 1 };
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await chat.GetResponseAsync([new ChatMessage(ChatRole.User, "ok")], ping, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogDebug("Keep-warm ping failed (Ollama down?): {Error}", ex.Message);
            }
            await Task.Delay(TimeSpan.FromMinutes(4), stoppingToken);
        }
    }
}
