using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Ingestion.Models;

/// <summary>
/// A single text chunk ready for embedding and Qdrant upsert.
/// Produced by ChunkingService, enriched by EmbeddingService.
/// </summary>
public sealed class ChunkRecord
{
    /// <summary>Stable Qdrant point ID — derived deterministically from FileId + ChunkIndex.</summary>
    public Guid ChunkId { get; init; }

    /// <summary>Zero-based position of this chunk within the file.</summary>
    public int ChunkIndex { get; init; }

    // Source metadata (written as Qdrant payload for filtering)
    public Guid FileId { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string HeadingBreadcrumb { get; init; } = string.Empty;
    public DateTime LastModifiedUtc { get; init; }

    /// <summary>The chunk text that gets embedded and stored.</summary>
    public string Content { get; init; } = string.Empty;
    public int TokenCount { get; init; }

    /// <summary>Dense embedding vector — populated by EmbeddingService.</summary>
    public float[]? DenseVector { get; set; }

    /// <summary>
    /// Sparse BM25 vector — term index → weight mapping.
    /// Populated by EmbeddingService alongside the dense vector.
    /// </summary>
    public Dictionary<uint, float>? SparseVector { get; set; }
}
