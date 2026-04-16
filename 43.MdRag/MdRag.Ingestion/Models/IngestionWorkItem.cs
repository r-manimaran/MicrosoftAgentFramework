using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Ingestion.Models;

/// <summary>
/// Work item placed on the ingestion channel by FileWatcherService.
/// </summary>
public sealed class IngestionWorkItem
{
    public string FilePath { get; init; } = string.Empty;
    public bool ForceReindex { get; init; }
    public DateTime EnqueuedAtUtc { get; init; } = DateTime.UtcNow;
}