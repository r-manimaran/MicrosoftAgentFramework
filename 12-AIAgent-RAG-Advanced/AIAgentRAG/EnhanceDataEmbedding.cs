using AIAgentRAG.Models;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIAgentRAG;

public static class EnhanceDataEmbedding
{
    public static async Task Embed(AzureOpenAIClient client, SqlServerCollection<Guid,MovieVectorStoreRecord> collection, Movie[] movieDataForRag)
    {
        ChatClientAgent genereAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
            .CreateAIAgent(
            instructions: """
              You are an expert in finding the Genre of the movie based on its title and plot.
              Pick a single genre based on the following:
              - Adventure
              - Sci-Fi,
              - Comedy,
              - Horror
              - Action
              - Romance
              - Thriller
            """);

        // Delete and re-create 
        await collection.EnsureCollectionDeletedAsync();
        await collection.EnsureCollectionExistsAsync();
        int counter = 0;

        foreach (Movie movie in movieDataForRag)
        {
            counter++;

            ChatClientAgentRunResponse<string> genreResponse = await genereAgent.RunAsync<string>($"what is the genre of this movie : {movie.GetTitleAndDetails()}?");

            string genre = genreResponse.Result;
            Console.Write($"\r Embedding movies:{counter} of {movieDataForRag.Length}");
            await collection.UpsertAsync(new MovieVectorStoreRecord
            {
                Id = Guid.NewGuid(),
                Title = movie.Title,
                Plot = movie.Plot,
                Rating = movie.Rating,
                Genre = genre,
                Year = movie.Year,
            });
        }
        Console.WriteLine();
        Console.WriteLine("\nEmbedding complete.");
    }
}
