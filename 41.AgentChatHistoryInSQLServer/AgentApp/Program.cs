using AgentApp;
using AgentApp.Extensions;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using System.ClientModel;

Console.WriteLine("Hello");
var client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new ApiKeyCredential(LLMConfig.ApiKey));

OpenAI.OpenAIClientOptions opts = new()
{
    Endpoint = new Uri(LLMConfig.Endpoint),
};
var azureClient = new AzureOpenAIClient(
    new Uri(LLMConfig.Endpoint),
    new ApiKeyCredential(LLMConfig.ApiKey));
using var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Debug);
    })
    .ConfigureServices(services =>
    {
        services.AddSqlServerVectorStore(o => LLMConfig.SqlConnectionString,
            o=> new Microsoft.SemanticKernel.Connectors.SqlServer.SqlServerVectorStoreOptions()
            {
                EmbeddingGenerator = azureClient
                    .GetEmbeddingClient(LLMConfig.EmbeddingModelName)
                    .AsIEmbeddingGenerator()
            });
       
    })
    .Build();

await host.StartAsync();

var vs = host.Services.GetRequiredService<SqlServerVectorStore>();

const string userId = "user2";
const string sessionId = "user2-main-session";

var sqlserverProvider = new ChatHistoryMemoryProvider(
        vs,
        collectionName: "ChatSession",
        vectorDimensions: 1536,
        session => new ChatHistoryMemoryProvider.State(
            storageScope: new() { UserId = userId, SessionId = sessionId },
            searchScope: new() { UserId = userId }));

// Verify embedding dimensions
var embeddingResult = await azureClient
    .GetEmbeddingClient(LLMConfig.EmbeddingModelName)
    .AsIEmbeddingGenerator()
    .GenerateAsync(new[] { "test" });
Console.WriteLine($"Embedding dims:{embeddingResult[0].Vector.Length}");

VectorStore vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions()
{
    EmbeddingGenerator = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
                        new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey))
                             .GetEmbeddingClient(LLMConfig.EmbeddingModelName).AsIEmbeddingGenerator()

});

var inMemoryProvider = new ChatHistoryMemoryProvider(
                vectorStore,
                collectionName: "chatHistory",
                vectorDimensions: 3072,
                 session => new ChatHistoryMemoryProvider.State(
                 storageScope: new() { UserId = "UID1", SessionId = Guid.NewGuid().ToString() },
                 searchScope: new() { UserId = "UID1" }));



var embeddingClient = azureClient.GetEmbeddingClient(LLMConfig.EmbeddingModelName);

var generator = embeddingClient.AsIEmbeddingGenerator();

// For testing to check the embedding
var result = await generator.GenerateAsync(new[] { "test message" });
Console.WriteLine("Embedding dims: " + result[0].Vector.Length); // Should be 1536

IChatClient agent = client.GetChatClient(LLMConfig.DeploymentOrModelId).AsIChatClient();
ChatClientAgentOptions agentOptions = new()
{
    Id = "1",
    Description = "Fan of Tamil Literature",
    Name = "Tamil Book Historian",
    ChatOptions = new ChatOptions() {
        Instructions = "You possess good knowledge of Tamil Literature. " +
                       "When recalling the user's favourite authors or books, " +
                       "use the context provided to you."
    },
    ChatHistoryProvider = new InMemoryChatHistoryProvider(),
    AIContextProviders = [ sqlserverProvider]
};

ChatClientAgent agent1 = agent.AsAIAgent(agentOptions);
Utils.WriteLineSuccess("---Session 1 -------");
AgentSession? session1 = await agent1.CreateSessionAsync();
Utils.WriteLineSuccess("Agent Created with Id: " + agentOptions.Id);
AgentResponse response1 = await agent1.RunAsync("I enjoyed reading books written by Sujatha. When was he born?", session1);
Console.WriteLine(response1);

Utils.WriteLineSuccess("--- Session 2------");
AgentSession? session2 = await agent1.CreateSessionAsync();
Console.WriteLine(await agent1.RunAsync("Can you list some books from my favorite author?", session2));

await host.StopAsync();
/*AgentSession? session2 = await agent1.CreateSessionAsync();
Console.WriteLine(await agent1.RunAsync("Can you tell me about my favorite author?", session2));*/
/*
static async Task WaitForMemoryFlushAsync(
    SqlServerVectorStore store,
    string userId,
    int timeoutSeconds = 30)
{
    var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
    while (DateTime.UtcNow < deadline)
    {
        try
        {
            var collection = store.GetCollection<string, ChatHistoryMemoryRecord>("ChatSession");
            // Simple existence check — if any record exists for this user, memories are written
            var search = await collection.VectorizedSearchAsync(
                new ReadOnlyMemory<float>(new float[1536]),  // dummy vector — we just want a count
                new VectorSearchOptions { Top = 1, Filter = r => r.UserId == userId });

            await foreach (var _ in search.Results) { return; } // found something
        }
        catch { /* collection may not exist yet }

        await Task.Delay(2000);
        Console.Write(".");
    }
    Console.WriteLine("\nWarning: memory flush timed out — session 2 may lack prior context.");
}*/
