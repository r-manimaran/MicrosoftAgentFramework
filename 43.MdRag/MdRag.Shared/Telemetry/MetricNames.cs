using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Shared.Telemetry;

public static class MetricNames
{
    public const string QueryLatencyMs = "rag.query.latency_ms";
    public const string PromptTokens = "rag.tokens.prompt";
    public const string CompletionTokens = "rag.tokens.completion";
    public const string QueryCostUSD = "rag.query.cost_usd";
    public const string RetrievedChunkCount = "rag.retrieval.chunk_count";
    public const string TopRelevanceScore = "rag.retrieval.top_relevance_score";
    public const string VectorSearchLatencyMs = "rag.retrieval.vector_search_latency_ms";
    public const string CacheHits = "rag.cache.hits";
    public const string CacheMisses = "rag.cache.misses";
    public const string FaithfulnessScore = "rag.eval.faithfulness_score";
    public const string AnswerRelevanceScore = "rag.eval.answer_relevance_score";
    public const string FilesIngested = "rag.ingestion.files_total";
    public const string ChunksPerFile = "rag.ingestion.chunks_per_file";
    public const string EmbeddingLatencyMs = "rag.ingestion.embedding_latency_ms";
}
