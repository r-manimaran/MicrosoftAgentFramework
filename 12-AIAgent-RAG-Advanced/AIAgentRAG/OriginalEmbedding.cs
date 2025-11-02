using AIAgentRAG.Models;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIAgentRAG;

public static class OriginalEmbedding
{
    public static async Task Embed(SqlServerCollection<Guid, MovieVectorStoreRecord> collection, Movie[] movieDataForRag)
    {
        await collection.EnsureCollectionDeletedAsync();
        await collection.EnsureCollectionExistsAsync();

        int counter = 0;

        foreach (var movie in movieDataForRag)
        {

            counter++;
            Console.Write($"\rEmbedding Movies: {counter}/{movieDataForRag.Length}");
            await collection.UpsertAsync(new MovieVectorStoreRecord
            {
                Id = Guid.NewGuid(),
                Title = movie.Title,
                Plot = movie.Plot,
                Rating = movie.Rating,
                Year = movie.Year
            });            
        }
        Console.WriteLine();
        Console.WriteLine("Embedding complete");
    }
}
