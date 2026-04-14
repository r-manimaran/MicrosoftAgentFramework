using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Contracts.Chat;

/// <summary>
/// Full response returned by the agent pipeline for a chat query.
/// Used for non-streaming (polling) responses. For streaming, see ChatStreamToken.
/// </summary>
/// <param name="SessionId">Echoed back so the client can correlate response to session.</param>
/// <param name="Answer">
///   The complete answer text produced by the Chat Agent.
///   May contain markdown formatting. The Blazor client renders this as HTML.
/// </param>
/// <param name="Citations">
///   Ordered list of source chunks the agent used to construct the answer.
///   The Blazor CitationPanel component renders these as expandable source cards.
/// </param>
/// <param name="Evaluation">
///   Optional quality scores produced asynchronously by the Eval Agent.
///   May be null if evaluation has not yet completed.
/// </param>
/// <param name="CacheHit">
///   True when the answer was served from the semantic cache (Redis).
///   Surfaced in the UI as a small badge so developers can verify cache behaviour.
/// </param>
/// <param name="LatencyMs">Total server-side latency in milliseconds.</param>
public sealed record ChatResponse(
    Guid SessionId,
    string Answer,
    IReadOnlyList<CitationDto> Citations,
    EvaluationDto Evaluation = null,
    bool CacheHit = false,
    int LatencyMs = 0);


/// <summary>
/// A single source chunk that contributed to the answer.
/// </summary>
/// <param name="ChunkId">Qdrant point ID — allows deep-linking to the raw chunk.</param>
/// <param name="FilePath">
///   Relative path of the source markdown file, e.g. "docs/api/endpoints.md".
/// </param>
/// <param name="HeadingBreadcrumb">
///   Full heading path within the file, e.g. "API Reference > Authentication > OAuth2".
///   Built by the Markdown parser from the heading hierarchy.
/// </param>
/// <param name="Preview">
///   First ~200 characters of the chunk text, shown in the citation panel.
/// </param>
/// <param name="RelevanceScore">
///   Cosine similarity score from Qdrant (0.0–1.0).
///   Higher = more semantically similar to the query.
/// </param>
/// <param name="RerankScore">
///   Cross-encoder re-ranking score (0–10) assigned by the Rerank Agent.
///   Null if reranking was skipped (e.g. single-chunk result).
/// </param>
public sealed record CitationDto(
    Guid ChunkId,
    string FilePath,
    string HeadingBreadcrumb,
    string Preview,
    double RelevanceScore,
    double? RerankScore =null);

/// <summary>
/// Quality scores produced by the Eval Agent after the answer is generated.
/// Logged as OTel metrics; also surfaced in the developer debug view.
/// </summary>
/// <param name="FaithfulnessScore">
///   0–10. Measures whether the answer is grounded in the retrieved sources
///   and does not introduce hallucinated facts.
/// </param>
/// <param name="RelevanceScore">
///   0–10. Measures whether the answer actually addresses the user's question.
/// </param>
/// <param name="EvaluatedAt">UTC timestamp when scores were computed.</param>
public sealed record EvaluationDto(
    double FaithfulnessScore,
    double RelevanceScore,
    DateTime EvaluatedAt
);