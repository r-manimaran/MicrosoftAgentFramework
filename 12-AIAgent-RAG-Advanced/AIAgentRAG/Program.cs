/*
 * Increase search results
 * Rephrase the question optimized for search
 * Enhance Embeddings
 * Filter in Search
 * Common Sense (Don't use AI in all scenarios)
 */
using AIAgentRAG;
using AIAgentRAG.Models;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using System.Text.Json;

string jsonWithMovies = await File.ReadAllTextAsync("");
Movie[] movieDataForRag = JsonSerializer.Deserialize<Movie[]>(jsonWithMovies)!;

AzureOpenAIClient client = new(new Uri(LLMConfig.Endpoint), new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));
IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = client.GetEmbeddingClient("text-embedding-3-small")
                                                                         .AsIEmbeddingGenerator();
SqlServerVectorStore vectorStore = new SqlServerVectorStore(LLMConfig.ConnectionString, new SqlServerVectorStoreOptions
{
    EmbeddingGenerator = embeddingGenerator,
});
SqlServerCollection<Guid, MovieVectorStoreRecord> collection = vectorStore.GetCollection<Guid, MovieVectorStoreRecord>("movies");


bool importData = false;
if(!await collection.CollectionExistsAsync())
{
    importData = true;
}
else
{
    Console.WriteLine("Re-import data?");
    ConsoleKeyInfo key = Console.ReadKey();
    if(key.Key == ConsoleKey.Y)
    {
        importData = true;
    }
}
ChatMessage question = new(ChatRole.User, "What is the 3 highest rated adventure movies?");
await Option1RephraseQuestion.Run(importData, movieDataForRag, question, client, collection);



