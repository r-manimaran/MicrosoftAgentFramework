using MdRag.Infrastructure.Repositories;
using MdRag.Ingestion.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Ingestion.Services;

/// <summary>
/// Orchestrates the full ingestion pipeline for a single file:
///   1. Hash check — skip if content unchanged (unless ForceReindex).
///   2. Parse       — extract sections and frontmatter via MarkdownParserService.
///   3. Chunk       — split sections into token-bounded chunks via ChunkingService.
///   4. Embed       — generate dense + sparse vectors via EmbeddingService.
///   5. Upsert      — write vectors + payloads to Qdrant via QdrantUpsertService.
///   6. Index       — update SQL file index with chunk count and Indexed status.
///
/// Each step updates the SQL status so the admin UI can show live progress.
/// </summary>
public sealed class IngestionPipelineService
{
    private readonly MarkdownParserService _parser;
    private readonly ChunkingService _chunker;
    private readonly EmbeddingService _embedder;
    private readonly QdrantUpsertService _upsertService;
    private readonly IFileIndexRepository _fileRepo;
    private readonly IngestionOptions _options;
    private readonly ILogger<IngestionPipelineService> _logger;
    public IngestionPipelineService(
        MarkdownParserService parser,
        ChunkingService chunker,
        EmbeddingService embedder,
        QdrantUpsertService upsertService,
        IFileIndexRepository fileRepo,
        IOptions<IngestionOptions> options,
        ILogger<IngestionPipelineService> logger)
    {
        _parser = parser;
        _chunker = chunker;
        _embedder = embedder;
        _upsertService = upsertService;
        _fileRepo = fileRepo;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(IngestionWorkItem item, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_options.MdFilesRootPath, item.FilePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found, skipping: {Path}", fullPath);
            return;
        }
    }


}
