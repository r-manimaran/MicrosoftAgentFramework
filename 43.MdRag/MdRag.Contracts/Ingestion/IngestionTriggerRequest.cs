using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Contracts.Ingestion;

/// <summary>
/// Posted to POST /ingest/trigger to manually kick off ingestion
/// for one or more files. Used by the admin FileIndexPage in Blazor.
/// </summary>
/// <param name="FilePaths">
///   Relative paths of files to re-ingest. When empty, all files in the
///   configured root folder are queued for re-ingestion (full re-index).
/// </param>
/// <param name="ForceReindex">
///   When true, bypasses the content-hash check and re-embeds even
///   files whose content has not changed since last indexing.
///   Useful after changing chunking strategy or embedding model.
/// </param>
public sealed record IngestionTriggerRequest(
    IReadOnlyList<string> FilePaths,
    bool ForceReindex = false
);

/// <summary>
/// Returned by POST /ingest/trigger.
/// </summary>
/// <param name="QueuedCount">Number of files added to the ingestion queue.</param>
/// <param name="SkippedCount">
///   Files skipped because their hash matched the indexed version
///   (only relevant when ForceReindex = false).
/// </param>
/// <param name="FileIds">
///   Mapping of file path → FileId so the caller can poll
///   GET /ingest/status/{fileId} for progress.
/// </param>
public sealed record IngestionTriggerResponse(
    int QueuedCount,
    int SkippedCount,
    IReadOnlyDictionary<string, Guid> FileIds
);