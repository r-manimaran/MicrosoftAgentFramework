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

var vs = host.Services.GetRequiredService<SqlServerVectorStore>();

var sqlserverProvider = new ChatHistoryMemoryProvider(
        vs,
        collectionName: "ChatSession",
        vectorDimensions: 1536,
        session => new ChatHistoryMemoryProvider.State(
            storageScope: new() { UserId = "user2", SessionId = Guid.NewGuid().ToString() },
            searchScope: new() { UserId = "user2" }));

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
    ChatOptions = new ChatOptions() { Instructions = "You posses a good knowledge on Tamil Literature" },
    ChatHistoryProvider = new InMemoryChatHistoryProvider(),
    AIContextProviders = [ sqlserverProvider]
};

ChatClientAgent agent1 = agent.AsAIAgent(agentOptions);
AgentSession? session1 = await agent1.CreateSessionAsync();
Utils.WriteLineSuccess("Agent Created with Id: " + agentOptions.Id);
AgentResponse response1 = await agent1.RunAsync("I enjoyed reading books written by Sujatha. When was he born?", session1);
Console.WriteLine(response1);

await host.StopAsync();

AgentSession? session2 = await agent1.CreateSessionAsync();
Console.WriteLine(await agent1.RunAsync("Can you list some books from my favorite author?", session2));
