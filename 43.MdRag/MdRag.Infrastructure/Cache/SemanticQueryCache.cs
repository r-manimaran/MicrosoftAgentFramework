using MdRag.Contracts.Chat;
using MdRag.Shared.Telemetry;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MdRag.Infrastructure.Cache;

public sealed class SemanticQueryCache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<SemanticQueryCache> _logger;
    private readonly SemanticCacheOptions _options;

    private const string KeysSetName = "mdrag:cache:keys";
    public SemanticQueryCache(IConnectionMultiplexer redis,
        IOptions<SemanticCacheOptions> options,
        ILogger<SemanticQueryCache> logger)
    {
        _redis = redis;
        _logger = logger;
        _options = options.Value;

    }

    public async Task<ChatResponse?> TryGetAsync(
        float[] queryEmbedding, CancellationToken ct = default)
    {
        using var activity = ActivitySources.Infrastructure
           .StartActivity("SemanticCache.TryGet");

        var db = _redis.GetDatabase();

        // Fetch all known cache keys from the tracking set
        var keys = await db.SetMembersAsync(KeysSetName);
        if (keys.Length == 0)
        {
            RagMeters.CacheMisses.Add(1);
            return null;
        }

        ChatResponse? bestMatch = null;
        double bestSimilarity = 0;

        foreach (var key in keys)
        {
            var raw = await db.StringGetAsync(key.ToString());
            if (!raw.HasValue) continue;

            var entry = JsonSerializer.Deserialize<CacheEntry>(raw!);
            if (entry is null) continue;

            var similarity = CosineSimilarity(queryEmbedding, entry.Embedding);
            if (similarity > bestSimilarity)
            {
                bestSimilarity = similarity;
                bestMatch = entry.Response;
            }
        }

        if (bestMatch is not null && bestSimilarity >= _options.SimilarityThreshold)
        {
            _logger.LogInformation(
                "Semantic cache HIT — similarity {Similarity:F4}", bestSimilarity);
            activity?.SetTag("cache.hit", true);
            activity?.SetTag("cache.similarity", bestSimilarity);
            RagMeters.CacheHits.Add(1);
            return bestMatch with { CacheHit = true };
        }

        activity?.SetTag("cache.hit", false);
        RagMeters.CacheMisses.Add(1);
        return null;
    }

    /// <summary>Stores a query embedding + response pair in Redis with TTL.</summary>
    public async Task SetAsync(
        float[] queryEmbedding, ChatResponse response, CancellationToken ct = default)
    {
        using var activity = ActivitySources.Infrastructure
            .StartActivity("SemanticCache.Set");

        var db = _redis.GetDatabase();
        var key = $"mdrag:cache:embedding:{Guid.NewGuid():N}";

        var entry = new CacheEntry(queryEmbedding, response);
        var json = JsonSerializer.Serialize(entry);

        await db.StringSetAsync(key, json, _options.CacheTtl);
        await db.SetAddAsync(KeysSetName, key);

        // Keep the tracking set from growing forever — remove expired keys
        // (best-effort; Redis TTL handles actual memory reclaim)
        _logger.LogDebug("Semantic cache SET key={Key}", key);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;

        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denom = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denom == 0 ? 0 : dot / denom;
    }

    private sealed record CacheEntry(float[] Embedding, ChatResponse Response);

}
