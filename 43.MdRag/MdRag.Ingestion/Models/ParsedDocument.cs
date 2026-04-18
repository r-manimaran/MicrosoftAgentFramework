using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Ingestion.Models;

/// <summary>
/// Result of parsing a single markdown file.
/// Produced by MarkdownParserService and consumed by ChunkingService.
/// </summary>
public sealed class ParsedDocument
{
    /// <summary>Unique file identifier from the SQL FileIndex.</summary>
    public Guid FileId { get; init; }

    /// <summary>Relative path from the MD files root folder.</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>SHA-256 hex hash of the raw file bytes.</summary>
    public string ContentHash { get; init; } = string.Empty;

    /// <summary>File's last-modified timestamp (UTC).</summary>
    public DateTime LastModifiedUtc { get; init; }

    /// <summary>
    /// Optional frontmatter metadata extracted from the YAML block at the
    /// top of the file (e.g. title, author, tags, version).
    /// </summary>
    public IReadOnlyDictionary<string, string> Frontmatter { get; init; }
        = new Dictionary<string, string>();

    /// <summary>
    /// Ordered list of top-level sections extracted by heading hierarchy.
    /// Each section maps to one or more chunks after ChunkingService runs.
    /// </summary>
    public IReadOnlyList<DocumentSection> Sections { get; init; }
        = new List<DocumentSection>();
}
