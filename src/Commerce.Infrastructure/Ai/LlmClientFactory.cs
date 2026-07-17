using Microsoft.Extensions.AI;
using OllamaSharp;

namespace Commerce.Infrastructure.Ai;

public sealed class LlmOptions
{
    /// <summary>ollama (local default) | openai | gemini — decision 5.</summary>
    public string Provider { get; set; } = "ollama";
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string ChatModel { get; set; } = "llama3.1:8b";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public string? ApiKey { get; set; }

    /// <summary>Per-request HTTP timeout. Local CPU inference with a cold model can take minutes.</summary>
    public int TimeoutSeconds { get; set; } = 600;

    /// <summary>Ping the local model every few minutes so it stays loaded (ollama only).</summary>
    public bool KeepWarm { get; set; } = true;
}

public static class LlmClientFactory
{
    public static IChatClient CreateChatClient(LlmOptions options) => options.Provider.ToLowerInvariant() switch
    {
        "ollama" => new OllamaApiClient(
            new HttpClient
            {
                BaseAddress = new Uri(options.Endpoint),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
            },
            options.ChatModel),
        "openai" => throw new NotSupportedException(
            "OpenAI toggle: add the Microsoft.Extensions.AI.OpenAI package and wire it here (Llm:ApiKey required)."),
        "gemini" => throw new NotSupportedException(
            "Gemini toggle: add the Google_GenerativeAI.Microsoft package and wire it here (Llm:ApiKey required)."),
        var other => throw new InvalidOperationException($"Unknown Llm:Provider '{other}'."),
    };
}
