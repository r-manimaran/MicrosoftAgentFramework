using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Contracts.Ingestion;

/// <summary>
/// Represents the current ingestion state of a single markdown file.
/// Polled by the Blazor UploadPage to show live per-file progress.
/// </summary>
/// <param name="FileId">Stable GUID assigned on first detection of the file.</param>
/// <param name="FilePath">
///   Path of the file relative to the configured MD files root folder.
///   Example: "guides/getting-started.md"
/// </param>
/// <param name="Status">Current pipeline stage.</param>
/// <param name="ChunkCount">
///   Number of chunks written to Qdrant. Zero until Status reaches Indexed.
/// </param>
/// <param name="LastModifiedUtc">File's last-modified timestamp from the filesystem.</param>
/// <param name="IndexedAtUtc">
///   When the file was successfully indexed. Null until Status = Indexed.
/// </param>
/// <param name="ErrorMessage">
///   Non-null when Status = Failed. Contains a user-friendly summary of what went wrong.
/// </param>
public sealed record FileIngestionStatus(
    Guid FileId,
    string FilePath,
    IngestionStage Status,
    int ChunkCount,
    DateTime LastModifiedUtc,
    DateTime? IndexedAtUtc = null,
    string? ErrorMessage = null
);

/// <summary>
/// Ordered pipeline stages for a single file.
/// The Blazor upload UI renders a step indicator based on this value.
/// </summary>
public enum IngestionStage
{
    /// <summary>File detected; waiting for a worker thread to pick it up.</summary>
    Queued,

    /// <summary>Markdig is parsing frontmatter and building the heading tree.</summary>
    Parsing,

    /// <summary>Parsed sections are being split into token-bounded chunks.</summary>
    Chunking,

    /// <summary>Chunks are being sent to Azure OpenAI for embedding.</summary>
    Embedding,

    /// <summary>Vectors and payloads are being upserted into Qdrant.</summary>
    Upserting,

    /// <summary>All chunks are in Qdrant; SQL file index has been updated.</summary>
    Indexed,

    /// <summary>
    /// A non-retryable error occurred. See FileIngestionStatus.ErrorMessage for details.
    /// The file will not be retried unless manually re-triggered.
    /// </summary>
    Failed
}
