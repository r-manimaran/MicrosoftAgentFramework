﻿using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIAgentRAG;

public class OriginalSearchTool(SqlServerCollection<Guid,MovieVectorStoreRecord> collection)
{
    public async Task<List<string>> SearchVectorStore(string question)
    {
        List<string> result = [];
        await foreach(VectorSearchResult<MovieVectorStoreRecord> searchResult in collection.SearchAsync(question, 25,
            new VectorSearchOptions<MovieVectorStoreRecord>
            {
                IncludeVectors = false
            }))
        {
            MovieVectorStoreRecord record = searchResult.Record;
            result.Add(record.GetTitleAndDetails());
        }
        return result;
    }
}
