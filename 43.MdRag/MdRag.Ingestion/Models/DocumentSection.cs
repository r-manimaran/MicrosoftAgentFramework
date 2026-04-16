using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Ingestion.Models;

/// <summary>
/// A logical section of a markdown document delimited by a heading (H1–H6).
/// </summary>
public sealed class DocumentSection
{
    /// <summary>
    /// Full heading breadcrumb path, e.g. "API Reference > Authentication > OAuth2".
    /// Built by MarkdownParserService as it walks the heading hierarchy.
    /// </summary>
    public string HeadingBreadcrumb { get; init; } = string.Empty;

    /// <summary>Heading level (1–6) of the section's own heading.</summary>
    public int HeadingLevel { get; init; }

    /// <summary>Plain-text content of this section (heading stripped).</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>Approximate token count of Content (pre-calculated by the parser).</summary>
    public int TokenCount { get; init; }
}
