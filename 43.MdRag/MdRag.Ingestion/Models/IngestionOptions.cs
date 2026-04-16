using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Ingestion.Models;

public sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";

    /// <summary>
    /// Root folder that FileWatcherService monitors recursively.
    /// Injected by AppHost as the MdFilesPath parameter.
    /// </summary>
    public string MdFilesRootPath { get; set; } = "./md-files";

    /// <summary>
    /// Maximum number of tokens per chunk.
    /// text-embedding-3-large supports up to 8,191 tokens;
    /// 512 is a good default for retrieval quality vs context usage.
    /// </summary>
    public int MaxTokensPerChunk { get; set; } = 512;

    /// <summary>
    /// Number of overlapping tokens between consecutive chunks.
    /// Overlap preserves context at chunk boundaries.
    /// </summary>
    public int ChunkOverlapTokens { get; set; } = 64;

    /// <summary>
    /// Maximum number of chunks to embed in a single Azure OpenAI API call.
    /// The API limit is 2048 inputs per request; 100 is conservative.
    /// </summary>
    public int EmbeddingBatchSize { get; set; } = 100;

    /// <summary>
    /// How many ingestion work items can be processed concurrently.
    /// Higher values increase throughput but also Azure OpenAI token consumption.
    /// </summary>
    public int MaxConcurrentIngestions { get; set; } = 3;

    /// <summary>
    /// Debounce delay after a file change event before queueing ingestion.
    /// Prevents multiple events for a single save operation.
    /// </summary>
    public TimeSpan FileChangeDebounce { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Maximum retry attempts for transient Azure OpenAI errors (429, 503).
    /// </summary>
    public int MaxEmbeddingRetries { get; set; } = 5;
}
