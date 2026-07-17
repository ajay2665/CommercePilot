using System.Text.Json;
using Commerce.Domain;
using Commerce.Domain.Entities;
using Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace Commerce.Infrastructure.Ai;

public sealed record VectorHit(
    KnowledgeSourceType SourceType, Guid SourceId, string Title, string Content, double Score);

/// <summary>
/// Decision 1's vector layer: embeddings persisted in SQL Server, cosine ranked
/// in-process over an in-memory cache (demo scale). Ingests products + KB
/// articles on first use via nomic-embed-text. If Ollama is unreachable the
/// service reports unavailable and callers fall back to keyword search.
/// </summary>
public sealed class EmbeddingService(
    IServiceScopeFactory scopeFactory,
    IOptions<LlmOptions> options,
    ILogger<EmbeddingService> logger)
{
    private sealed record CachedChunk(
        KnowledgeSourceType SourceType, Guid SourceId, string Title, string Content, float[] Vector);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<CachedChunk>? _cache;
    private IEmbeddingGenerator<string, Embedding<float>>? _generator;

    public bool IsAvailable { get; private set; }

    public async Task EnsureReadyAsync(CancellationToken ct = default)
    {
        if (_cache is not null) return;
        await _gate.WaitAsync(ct);
        try
        {
            if (_cache is not null) return;

            _generator ??= new OllamaApiClient(
                new HttpClient
                {
                    BaseAddress = new Uri(options.Value.Endpoint),
                    Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds),
                },
                options.Value.EmbeddingModel);

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();

            if (!await db.Embeddings.AnyAsync(ct))
                await IngestAsync(db, ct);

            var rows = await db.Embeddings.AsNoTracking().ToListAsync(ct);
            _cache = rows.Select(r => new CachedChunk(
                r.SourceType, r.SourceId, TitleFrom(r.MetadataJson), r.Content, FromBytes(r.Vector))).ToList();
            IsAvailable = true;
            logger.LogInformation("Vector cache ready: {Count} chunks", _cache.Count);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException)
        {
            logger.LogWarning("Embedding service unavailable ({Error}) — semantic search falls back to keywords", ex.Message);
            _cache = [];
            IsAvailable = false;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<VectorHit>> SearchAsync(
        string query, KnowledgeSourceType[]? sourceTypes, int topK, CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        if (!IsAvailable || _cache is null or { Count: 0 } || _generator is null)
            return [];

        try
        {
            var embedded = await _generator.GenerateAsync([query], null, ct);
            float[] queryVector = embedded[0].Vector.ToArray();

            IEnumerable<CachedChunk> scope = sourceTypes is { Length: > 0 }
                ? _cache.Where(c => sourceTypes.Contains(c.SourceType))
                : _cache;

            return scope
                .Select(c => new VectorHit(c.SourceType, c.SourceId, c.Title, c.Content, Cosine(queryVector, c.Vector)))
                .OrderByDescending(h => h.Score)
                .Take(topK)
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException)
        {
            logger.LogWarning("Query embedding failed: {Error}", ex.Message);
            return [];
        }
    }

    /// <summary>Embeds every product + knowledge article (one chunk each at this scale).</summary>
    private async Task IngestAsync(CommerceDbContext db, CancellationToken ct)
    {
        var products = await db.Products.AsNoTracking().ToListAsync(ct);
        var categories = (await db.Categories.AsNoTracking().ToListAsync(ct)).ToDictionary(c => c.Id, c => c.Name);
        var articles = await db.KnowledgeArticles.AsNoTracking().ToListAsync(ct);

        var items = new List<(KnowledgeSourceType Type, Guid Id, string Title, string Content, string Brand)>();
        foreach (var p in products)
        {
            string category = p.CategoryId is { } cid ? categories.GetValueOrDefault(cid, "") : "";
            items.Add((KnowledgeSourceType.Product, p.Id, p.Name,
                $"{p.Name}. {p.Description} Brand: {p.Brand}. Category: {category}. Price: {p.Price:F2} EUR.", p.Brand));
        }
        foreach (var a in articles)
            items.Add((a.SourceType, a.Id, a.Title, $"{a.Title}. {a.Body}", a.Brand));

        logger.LogInformation("Embedding {Count} chunks via {Model}…", items.Count, options.Value.EmbeddingModel);
        var vectors = await _generator!.GenerateAsync(items.Select(i => i.Content), null, ct);

        for (int i = 0; i < items.Count; i++)
        {
            db.Embeddings.Add(new EmbeddingChunk
            {
                SourceType = items[i].Type,
                SourceId = items[i].Id,
                ChunkIndex = 0,
                Content = items[i].Content,
                MetadataJson = JsonSerializer.Serialize(new { title = items[i].Title, brand = items[i].Brand }),
                Vector = ToBytes(vectors[i].Vector),
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private static string TitleFrom(string metadataJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            return doc.RootElement.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
        }
        catch (JsonException)
        {
            return "";
        }
    }

    private static double Cosine(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0;
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        return na == 0 || nb == 0 ? 0 : dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    private static byte[] ToBytes(ReadOnlyMemory<float> vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector.ToArray(), 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] FromBytes(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }
}
