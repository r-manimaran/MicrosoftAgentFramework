using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.SqlServer;

namespace ChatApp;

public sealed class DocumentChunk
{
    [VectorStoreKey]
    public Guid Key { get; set; } 
    [VectorStoreData]
    public string Content { get; set; } = string.Empty;
    [VectorStoreData]
    public string DocumentId { get; set; } = string.Empty;
    //[VectorStoreData]
    //public int ChunkIndex { get; set; }
    /// <summary>The 1536-dim embedding vector.</summary>
    [VectorStoreVector(Dimensions: 1536)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}
