using MdRag.Ingestion.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MdRag.Ingestion.Services;

public sealed class EmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly IngestionOptions _options;

    public EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IOptions<IngestionOptions> options,
        ILogger<EmbeddingService> logger)
    {
        _embeddingGenerator = embeddingGenerator;
        _logger = logger;
        _options = options.Value;
    }

    public async Task EmbedAsync(IReadOnlyList<ChunkRecord> chunks, CancellationToken ct = default)
    {
        if (chunks.Count == 0) return;

        var sw = Stopwatch.StartNew();

        var batches = chunks
            .Select((chunk, i) => (chunk, i))
            .GroupBy(x => x.i / _options.EmbeddingBatchSize)
            .Select(g => g.Select(x => x.chunk).ToList());

        foreach(var batch in batches)
        {

        }
    }

}
