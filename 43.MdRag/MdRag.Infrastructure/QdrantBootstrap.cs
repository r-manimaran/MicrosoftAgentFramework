using MdRag.Shared.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace MdRag.Infrastructure;

/// <summary>
/// Run-once startup service that ensures the Qdrant collection exists with
/// the correct vector dimensions, distance metric, and sparse vector config.
///
/// Called from MdRag.Api and MdRag.Ingestion Program.cs via:
///   await app.Services.GetRequiredService&lt;QdrantBootstrap&gt;().EnsureCollectionAsync();
///
/// Idempotent — safe to call on every restart. If the collection already
/// exists with the correct config it is a no-op.
/// </summary>
public sealed class QdrantBootstrap
{
    private readonly QdrantClient _qdrant;
    private readonly QdrantOptions _options;
    private readonly ILogger<QdrantBootstrap> _logger;

    public QdrantBootstrap(QdrantClient qdrant,
        IOptions<QdrantOptions> options,
        ILogger<QdrantBootstrap> logger)
    {
        _qdrant = qdrant;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureCollectionAsync(CancellationToken ct = default)
    {

        using var activity = ActivitySources.Infrastructure
            .StartActivity("QdrantBootstrap.EnsureCollection");

        var exists = await _qdrant.CollectionExistsAsync(_options.CollectionName, ct);
        if (exists)
        {
            _logger.LogInformation(
                "Qdrant collection '{Collection}' already exists — skipping bootstrap.",
                _options.CollectionName);
            return;
        }

        _logger.LogInformation(
            "Creating Qdrant collection '{Collection}' (dims={Dims}, distance={Distance})",
            _options.CollectionName, _options.VectorDimensions, _options.DistanceMetric);

        await _qdrant.CreateCollectionAsync(
            collectionName: _options.CollectionName,
            vectorsConfig: new VectorsConfig
            {
                // Dense vector (from Azure OpenAI embedding model)
                Params = new VectorParams
                {
                    Size = (ulong)_options.VectorDimensions,
                    Distance = _options.DistanceMetric,
                    OnDisk = true,   // keep large indexes off RAM
                }
            },
            // Sparse vectors for BM25 hybrid search
            sparseVectorsConfig: new SparseVectorConfig
            {
                Map =
                {
                    [_options.SparseVectorName] = new SparseVectorParams
                    {
                        Index = new SparseIndexConfig { OnDisk = true }
                    }
                }
            },
            cancellationToken: ct);

        // Create a payload index on file_path for fast metadata filtering
        await _qdrant.CreatePayloadIndexAsync(
            collectionName: _options.CollectionName,
            fieldName: "file_path",
            schemaType: PayloadSchemaType.Keyword,
            cancellationToken: ct);

        // Index on last_modified for date-range filters
        await _qdrant.CreatePayloadIndexAsync(
            collectionName: _options.CollectionName,
            fieldName: "last_modified_utc",
            schemaType: PayloadSchemaType.Datetime,
            cancellationToken: ct);

        _logger.LogInformation(
            "Qdrant collection '{Collection}' created successfully.", _options.CollectionName);
    }
}

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string CollectionName { get; set; } = "md-documents";

    /// <summary>
    /// Must match the embedding model output dimension.
    /// text-embedding-3-large = 3072, text-embedding-3-small = 1536.
    /// </summary>
    public int VectorDimensions { get; set; } = 3072;

    public Distance DistanceMetric { get; set; } = Distance.Cosine;

    /// <summary>Name of the sparse vector field used for BM25 hybrid search.</summary>
    public string SparseVectorName { get; set; } = "bm25";
}