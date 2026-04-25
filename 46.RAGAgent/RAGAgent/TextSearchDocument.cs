using Microsoft.Extensions.VectorData;
using System;
using System.Collections.Generic;
using System.Text;

namespace RAGAgent;

public class TextSearchDocument
{
    [VectorStoreKey]
    public string SourceId { get; set; } = string.Empty;
    [VectorStoreData]
    public string SourceName { get; set;  } = string.Empty;
    [VectorStoreData]
    public string SourceLink { get; set; } = string.Empty;
    [VectorStoreData]    
    public string Text { get; set;  } = string.Empty;
    [VectorStoreVector(Dimensions: 3072)]
    public string Embedding { get; set; } = string.Empty;
}
