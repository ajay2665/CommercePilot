using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

namespace Commerce.Infrastructure.Ai;

public sealed class LlmOptions
{
    /// <summary>ollama (local default) | openai | gemini — decision 5.</summary>
    public string Provider { get; set; } = "ollama";
    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>Active models — must match the provider (ollama: llama3.1:8b · openai: gpt-5-mini / gpt-5).</summary>
    public string ChatModel { get; set; } = "llama3.1:8b";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";

    /// <summary>For cloud providers. Prefer appsettings.Development.json (git-ignored) or OPENAI_API_KEY env var.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Per-request HTTP timeout. Local CPU inference with a cold model can take minutes.</summary>
    public int TimeoutSeconds { get; set; } = 600;

    /// <summary>Ping the local model every few minutes so it stays loaded (ollama only).</summary>
    public bool KeepWarm { get; set; } = true;

    /// <summary>USD per 1M tokens for ai_interactions cost tracking; leave 0 for local models.</summary>
    public decimal InputCostPer1M { get; set; }
    public decimal OutputCostPer1M { get; set; }

    public bool IsOllama => string.Equals(Provider, "ollama", StringComparison.OrdinalIgnoreCase);

    public decimal CostFor(long inputTokens, long outputTokens)
        => inputTokens / 1_000_000m * InputCostPer1M + outputTokens / 1_000_000m * OutputCostPer1M;
}

public static class LlmClientFactory
{
    public static IChatClient CreateChatClient(LlmOptions options) => options.Provider.ToLowerInvariant() switch
    {
        "ollama" => new OllamaApiClient(HttpFor(options), options.ChatModel),
        "openai" => new OpenAIClient(ResolveApiKey(options)).GetChatClient(options.ChatModel).AsIChatClient(),
        "gemini" => throw new NotSupportedException(
            "Gemini toggle: add the Google_GenerativeAI.Microsoft package and wire it here (Llm:ApiKey required)."),
        var other => throw new InvalidOperationException($"Unknown Llm:Provider '{other}'."),
    };

    public static IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(LlmOptions options)
        => options.Provider.ToLowerInvariant() switch
        {
            "ollama" => new OllamaApiClient(HttpFor(options), options.EmbeddingModel),
            "openai" => new OpenAIClient(ResolveApiKey(options))
                .GetEmbeddingClient(options.EmbeddingModel)
                .AsIEmbeddingGenerator(),
            var other => throw new InvalidOperationException(
                $"Llm:Provider '{other}' has no embedding generator wired."),
        };

    private static HttpClient HttpFor(LlmOptions options) => new()
    {
        BaseAddress = new Uri(options.Endpoint),
        Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
    };

    // IsNullOrWhiteSpace, not ?? — the key may exist as "" in appsettings and
    // must still fall through to the environment variable (reference-project lesson).
    private static string ResolveApiKey(LlmOptions options)
    {
        string? key = !string.IsNullOrWhiteSpace(options.ApiKey)
            ? options.ApiKey.Trim()
            : Environment.GetEnvironmentVariable("OPENAI_API_KEY")?.Trim();

        return !string.IsNullOrWhiteSpace(key)
            ? key
            : throw new InvalidOperationException(
                "Llm:Provider is 'openai' but no API key found.\n" +
                "  Option A: set Llm:ApiKey in src/Commerce.Api/appsettings.Development.json (git-ignored)\n" +
                "  Option B: set the OPENAI_API_KEY environment variable");
    }
}
