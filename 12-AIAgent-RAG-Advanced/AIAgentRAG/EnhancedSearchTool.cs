using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIAgentRAG;

public class EnhancedSearchTool(SqlServerCollection<Guid, MovieVectorStoreRecord> collection)
{
    public async Task<List<string>> SearchVectorSearch(string question, string genre)
    {
        List<string> results = [];
        await foreach(VectorSearchResult<MovieVectorStoreRecord> searchResult in collection.SearchAsync(question, 100,
            new VectorSearchOptions<MovieVectorStoreRecord>
            {
                IncludeVectors = false,
                Filter = record=> record.Genre == genre
            }))
        {
            MovieVectorStoreRecord record = searchResult.Record;
            results.Add(record.GetTitleAndDetails());
        }
        return results;
    }
    
}
