using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Contracts.Ingestion;

/// <summary>
/// Summary of all files known to the ingestion system.
/// Returned by GET /ingest/files and rendered in the Blazor FileIndexPage.
/// </summary>
/// <param name="TotalFiles">Total number of markdown files tracked.</param>
/// <param name="IndexedFiles">Files successfully in Qdrant.</param>
/// <param name="FailedFiles">Files currently in Failed state.</param>
/// <param name="PendingFiles">Files currently queued or in-progress.</param>
/// <param name="TotalChunks">Sum of chunks across all indexed files.</param>
/// <param name="LastIndexedAtUtc">Most recent successful indexing timestamp across all files.</param>
/// <param name="Files">Per-file status list.</param>
public sealed record FileIndexSummary(
    int TotalFiles,
    int IndexedFiles,
    int FailedFiles,
    int PendingFiles,
    int TotalChunks,
    DateTime? LastIndexedAtUtc,
    IReadOnlyList<FileIngestionStatus> Files
);