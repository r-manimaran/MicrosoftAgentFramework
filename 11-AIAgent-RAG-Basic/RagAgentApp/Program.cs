using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents.Indexes;
using Microsoft.Agents.AI;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.CosmosNoSql;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using OpenAI;
using OpenAI.Chat;
using RagAgentApp;
using RagAgentApp.Models;
using System.Text;
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

//AzureAISearchVectorStore vectorStoreAzSql = new AzureAISearchVectorStore(
//        new SearchIndexClient(new Uri("azureAISearchEndpoint"),
//   new AzureKeyCredential("azureAiSearchKey")));

//SqlServerVectorStore vectorStoreFromSqlServer2025 = new SqlServerVectorStore("connectionString");

//CosmosNoSqlVectorStore vectorStoreCosmos = new CosmosNoSqlVectorStore("connectionString", "databasename",
//    new CosmosClientOptions
//    {
//        UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Default
//    });

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

await foreach (VectorSearchResult<MovieVectorStoreRecord> searchResult in collection.SearchAsync(question.Text, 5,
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

Console.ReadLine();
//Console.Clear();

Utils.WriteLineSuccess("Scenario 3: RAG with AIAgent Toolkit");
SearchTool searchTool = new SearchTool(collection);
AIAgent aiAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
                    .CreateAIAgent(instructions:"You are an expert on a set of fictious movies given to you. (don't have any idea about real world movies",
                    tools: [AIFunctionFactory.Create(searchTool.SearchVectorStore)])
                    .AsBuilder()
                    .Use(FunctionCallingMiddleware)
                    .Build();
AgentRunResponse response3 = await aiAgent.RunAsync(question);
Console.WriteLine(response3);
Utils.WriteLineInformation("************************************");
Utils.WriteLineInformation($"- Input Tokens:{response3.Usage?.InputTokenCount}");
Utils.WriteLineInformation($"- Output Tokens:{response3.Usage?.OutputTokenCount}" +
    $"({response3.Usage?.GetOutputTokensUsedForReasoning()} was used for reasoning)");

Console.ReadLine();
async ValueTask<object?> FunctionCallingMiddleware(
    AIAgent callingAgent,
    FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken token)
{
    StringBuilder functionCallDetails = new();
    // Log the function tool which is being called
    functionCallDetails.Append($"Tool Call:'{context.Function.Name}'");
    // Log the arguments if any when calling the tool
    if (context.Arguments.Count > 0)
    {
        functionCallDetails.Append($" (Args: {string.Join(", ", context.Arguments.Select(kv => $"{kv.Key}:{kv.Value}"))})");
    }
    Utils.WriteLineInformation(functionCallDetails.ToString());
    return await next(context, token);
}

class SearchTool(InMemoryCollection<Guid, MovieVectorStoreRecord> collection)
{
    public async Task<List<string>> SearchVectorStore(string question)
    {
        List<string> result = [];
        await foreach (VectorSearchResult<MovieVectorStoreRecord> searchResult in collection.SearchAsync(question, 5,
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

