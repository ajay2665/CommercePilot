using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

namespace StorePilot.Infrastructure.Ai;

/// <summary>Everything specific to the local Ollama server.</summary>
public sealed class OllamaOptions
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string ChatModel { get; set; } = "llama3.1:8b";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";

    /// <summary>Per-request HTTP timeout. Local CPU inference with a cold model can take minutes.</summary>
    public int TimeoutSeconds { get; set; } = 600;

    /// <summary>Ping the model every few minutes so it stays loaded (ModelKeepWarmWorker).</summary>
    public bool KeepWarm { get; set; } = true;
}

/// <summary>Everything specific to OpenAI's cloud API.</summary>
public sealed class GptOptions
{
    public string ChatModel { get; set; } = "gpt-5-mini";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>Prefer appsettings.Development.json (git-ignored) or the OPENAI_API_KEY env var.</summary>
    public string? ApiKey { get; set; }

    /// <summary>USD per 1M tokens for ai_interactions cost tracking.</summary>
    public decimal InputCostPer1M { get; set; }
    public decimal OutputCostPer1M { get; set; }
}

public sealed class LlmOptions
{
    /// <summary>Chat provider — the single source of truth: ollama (local default) | openai | gemini.</summary>
    public string Provider { get; set; } = "ollama";

    /// <summary>
    /// Embedding provider — independent of Provider, so chat and embeddings can
    /// run on different providers at once (e.g. GPT for chat, local Ollama for
    /// embeddings). ollama | openai.
    /// </summary>
    public string EmbeddingProvider { get; set; } = "ollama";

    public OllamaOptions Ollama { get; set; } = new();
    public GptOptions Gpt { get; set; } = new();

    public bool IsOllamaChat => string.Equals(Provider, "ollama", StringComparison.OrdinalIgnoreCase);

    public bool IsEmbeddingOllama
        => string.Equals(EmbeddingProvider, "ollama", StringComparison.OrdinalIgnoreCase);

    /// <summary>Resolves to the model tag for whichever provider is actually active.</summary>
    public string ChatModel => IsOllamaChat ? Ollama.ChatModel : Gpt.ChatModel;
    public string EmbeddingModel => IsEmbeddingOllama ? Ollama.EmbeddingModel : Gpt.EmbeddingModel;

    /// <summary>Ollama chat is free; only Gpt's per-token rates ever apply.</summary>
    public decimal CostFor(long inputTokens, long outputTokens)
        => IsOllamaChat
            ? 0m
            : inputTokens / 1_000_000m * Gpt.InputCostPer1M + outputTokens / 1_000_000m * Gpt.OutputCostPer1M;
}

public static class LlmClientFactory
{
    public static IChatClient CreateChatClient(LlmOptions options)
    {
        var provider = options.Provider.ToLowerInvariant();
        return provider switch
        {
            "ollama" => new OllamaApiClient(HttpFor(options.Ollama), options.Ollama.ChatModel),
            "openai" or "gpt" => new OpenAIClient(ResolveApiKey(options.Gpt)).GetChatClient(options.Gpt.ChatModel).AsIChatClient(),
            "gemini" => throw new NotSupportedException(
                "Gemini toggle: add the Google_GenerativeAI.Microsoft package and wire it here (Llm:Gpt:ApiKey required)."),
            var other => throw new InvalidOperationException($"Unknown Llm:Provider '{other}'."),
        };
    }

    /// <summary>
    /// Embeddings follow their own Llm:EmbeddingProvider toggle, independent of
    /// chat's Provider — e.g. GPT for chat with Ollama still doing embeddings
    /// for free, or both on the same provider.
    /// </summary>
    public static IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(LlmOptions options)
        => options.EmbeddingProvider.ToLowerInvariant() switch
        {
            "ollama" => new OllamaApiClient(HttpFor(options.Ollama), options.Ollama.EmbeddingModel),
            "openai" or "gpt" => new OpenAIClient(ResolveApiKey(options.Gpt))
                .GetEmbeddingClient(options.Gpt.EmbeddingModel)
                .AsIEmbeddingGenerator(),
            var other => throw new InvalidOperationException($"Unknown Llm:EmbeddingProvider '{other}'."),
        };

    private static HttpClient HttpFor(OllamaOptions ollama) => new()
    {
        BaseAddress = new Uri(ollama.Endpoint),
        Timeout = TimeSpan.FromSeconds(ollama.TimeoutSeconds),
    };

    // IsNullOrWhiteSpace, not ?? — the key may exist as "" in appsettings and
    // must still fall through to the environment variable (reference-project lesson).
    private static string ResolveApiKey(GptOptions gpt)
    {
        string? key = !string.IsNullOrWhiteSpace(gpt.ApiKey)
            ? gpt.ApiKey.Trim()
            : Environment.GetEnvironmentVariable("OPENAI_API_KEY")?.Trim();

        return !string.IsNullOrWhiteSpace(key)
            ? key
            : throw new InvalidOperationException(
                "Llm:Provider (or EmbeddingProvider) is 'openai' but no API key found.\n" +
                "  Option A: set Llm:Gpt:ApiKey in src/StorePilot.Api/appsettings.Development.json (git-ignored)\n" +
                "  Option B: set the OPENAI_API_KEY environment variable");
    }
}
