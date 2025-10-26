using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents.Indexes;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using OpenAI;
using OpenAI.Chat;
using RagAgentApp;
using RagAgentApp.Models;
using System.Text.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
                        new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));

ChatClient chatClient = client.GetChatClient(LLMConfig.DeploymentOrModelId);

string jsonMovies = await File.ReadAllTextAsync("mymovies.json");

Movie[] moviesDataForRAG = JsonSerializer.Deserialize<Movie[]>(jsonMovies)!;

ChatMessage question = new ChatMessage(ChatRole.User, "List 3 highest rated adventure movies (list their title, plot, year and rating ?");

ChatClientAgent agent = chatClient.CreateAIAgent(instructions: "You are an expert on a set of fictious movies given to you. (don't have any idea about real world movies");

Utils.WriteLineSuccess("Scenario 1: RAG with Azure OpenAI Service");
List<ChatMessage> preLoadedEverythingChatMessages = [
      new(ChatRole.Assistant, "Here are all the movies")
    ];

foreach(Movie movie in moviesDataForRAG)
{
    preLoadedEverythingChatMessages.Add(new ChatMessage(ChatRole.Assistant, movie.GetTitleAndDetails()));
}

preLoadedEverythingChatMessages.Add(question);
AgentRunResponse response1 = await agent.RunAsync(preLoadedEverythingChatMessages);
Console.WriteLine(response1);

Utils.WriteLineInformation("************************************");
Utils.WriteLineInformation($"- Input Tokens:{response1.Usage?.InputTokenCount}");
Utils.WriteLineInformation($"- Output Tokens:{response1.Usage?.OutputTokenCount}" +
    $"({response1.Usage?.GetOutputTokensUsedForReasoning()} was used for reasoning)");
Utils.Separator();

Console.ReadLine();
//Console.Clear();

Utils.WriteLineSuccess("Scenario 2: RAG with Embeddings and Azure OpenAI Service");
IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = client.GetEmbeddingClient("text-embedding-3-small")
                            .AsIEmbeddingGenerator();

InMemoryVectorStore vectorStore = new(new InMemoryVectorStoreOptions
{
    EmbeddingGenerator = embeddingGenerator
});

//AzureAISearchVectorStore vectorStoreAzSql = new SearchIndexClient(new Uri("azureAISearchEndpoint"),
//    new AzureKeyCredential("azureAiSearchKey"));
//SqlServerVectorStore vectorStoreFromSqlServer2025 = new ()

InMemoryCollection<Guid, MovieVectorStoreRecord> collection = vectorStore.GetCollection<Guid, MovieVectorStoreRecord>("movies");
await collection.EnsureCollectionExistsAsync();

int counter = 0;
foreach(Movie movie in moviesDataForRAG)
{
    counter++;
    Console.Write($"\rEmbedding movies: {counter}/{moviesDataForRAG.Length}");
    await collection.UpsertAsync(new MovieVectorStoreRecord
    {
        Id = Guid.CreateVersion7(),
        Title = movie.Title,
        Plot = movie.Plot,
        Year = movie.Year,
        Rating = movie.Rating      
    });
}
Console.WriteLine();
Console.WriteLine("\rEmbedding movies: Done.. Lets ask the questions again using RAG");

List<ChatMessage> ragPreloadedChatMessages = [ new ChatMessage(ChatRole.Assistant, "Here are the more relevant movies") ];

await foreach (VectorSearchResult<MovieVectorStoreRecord> searchResult in collection.SearchAsync(question.Text, 10,
    new VectorSearchOptions<MovieVectorStoreRecord>
    {
        IncludeVectors = false
    }))
{
    MovieVectorStoreRecord record = searchResult.Record;
    ragPreloadedChatMessages.Add(new ChatMessage(ChatRole.Assistant, record.GetTitleAndDetails()));
}
ragPreloadedChatMessages.Add(question);

AgentRunResponse response2 = await agent.RunAsync(ragPreloadedChatMessages);
Console.WriteLine(response2);

Utils.WriteLineInformation("************************************");
Utils.WriteLineInformation($"- Input Tokens:{response2.Usage?.InputTokenCount}");
Utils.WriteLineInformation($"- Output Tokens:{response2.Usage?.OutputTokenCount}" +
    $"({response2.Usage?.GetOutputTokensUsedForReasoning()} was used for reasoning)");
Utils.Separator();


