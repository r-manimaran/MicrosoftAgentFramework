using Microsoft.Extensions.VectorData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIAgentRAG;

public class MovieVectorStoreRecord
{
    [VectorStoreKey]
    public required Guid Id { get; set; }
    [VectorStoreData]
    public required string Title { get; set; }
    [VectorStoreData]
    public required string Plot { get; set; }
    [VectorStoreData]
    public required decimal Rating { get; set; }
    [VectorStoreData]
    public required int Year { get; set; }

    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineDistance, IndexKind = IndexKind.Hnsw)]
    public string? Embedding => $"Title: {Title} - Rating: {Rating} - Plot:{Plot}";

    public string GetTitleAndDetails()
    {
        return $"Title: {Title} - Rating: {Rating} - Plot:{Plot}";
    }
}
