using AgenticRAGWebApi.Models;
using Microsoft.Extensions.AI;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AgenticRAGWebApi.Services;

public class HybridSearchService(QdrantClient qdrant,
                                 IEmbeddingGenerator<string,Embedding<float>> embedder)
{
    private const string Collection = "it_runbooks";

    // --Hybrid search: dense + sparse --> RRF --------------
    public async Task<List<HybridSearchResult>> SearchAsync(
        string query,
        float semanticWeight =0.5f, // Agent can tune this per call
        int topK=4)
    {
        // 1. Generate dense embedding
        var embedding = await embedder.GenerateVectorAsync(query);

        // 2. Build sparse vector (BM25-style token weights)
        var sparseVector = BuildSparseVector(query);

        // 3. Qdrant Prefetch: run both legs, fuse with RRF
        var points = await qdrant.QueryAsync(
            collectionName: Collection,
            prefetch:
            [
                // Dense leg - semantic similarity
                new Qdrant.Client.Grpc.PrefetchQuery {
                    Query = new Qdrant.Client.Grpc.Query { Nearest = new Qdrant.Client.Grpc.VectorInput(embedding.ToArray())},
                    Using = "dense",
                    Limit =(ulong)(topK * 4),
                    Params = new Qdrant.Client.Grpc.SearchParams { HnswEf = 128}
                },
                // Sparse leg - keyword /BM25
                new PrefetchQuery{
                    Query = new Query {
                        Nearest = new VectorInput(sparseVector)
                    },

                }
                ])
    }
}
