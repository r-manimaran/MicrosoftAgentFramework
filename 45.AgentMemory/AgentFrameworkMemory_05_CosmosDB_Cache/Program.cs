using AgentWithMemory_06_CosmosDB_Cache;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using Shared;
using System.Collections.ObjectModel;
using Embedding = Microsoft.Azure.Cosmos.Embedding;

// Register the Logger
var logger = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information);
}).CreateLogger("CosmosDB");

var client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
                new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));

// -------------------------
// Declare the Cosmos Client
// -------------------------
var cosmosClient = new CosmosClient(LLMConfig.AzureCosmosEndpoint, LLMConfig.AzureCosmosKey, new CosmosClientOptions
{
    CustomHandlers = { new CosmosTelemetryHandler(logger) }
});
                    
var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(LLMConfig.AzureCosmosDB);

var containerProperties = new ContainerProperties(
    id: "cache",
    partitionKeyPath: "/pk" 
);

// ✅ Vector Embedding Policy (matches your portal config)
containerProperties.VectorEmbeddingPolicy = new VectorEmbeddingPolicy(
    new Collection<Microsoft.Azure.Cosmos.Embedding>
    {
        new Embedding
        {
            Path = "/embedding",
            DataType = VectorDataType.Float32,
            DistanceFunction = DistanceFunction.Cosine,
            Dimensions = 1536
        }
    }
);

// ✅ Vector Index Policy (diskANN with search list size 100)
containerProperties.IndexingPolicy.VectorIndexes.Add(new VectorIndexPath
{
    Path = "/embedding",
    Type = VectorIndexType.DiskANN,
});



var cacheContainer = await database.Database.CreateContainerIfNotExistsAsync(containerProperties);
var historyContainer = database.Database.GetContainer("chathistory");
    
//var container = database.Database.GetContainer("cache"); // cache

// ---------------------
// Embedding Generator
// --------------------
IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = client
        .GetEmbeddingClient("text-embedding-3-small")
        .AsIEmbeddingGenerator();

// ----------------
// History provider
// ----------------
string conversationId = "AgentConversation";

var historyProvider = new CosmosChatHistoryProvider(
    cosmosClient: cosmosClient,
    databaseId: database.Database.Id,
    containerId: historyContainer.Id,
    stateInitializer: _ => new CosmosChatHistoryProvider.State(
        conversationId: conversationId,
        tenantId: "default-tenant",
        userId: "user1"),
    ownsClient: false
   );


var agentOptions = new ChatClientAgentOptions();
agentOptions.ChatHistoryProvider = historyProvider;

# pragma warning disable MAAI001

var compactionStrategy = new SummarizationCompactionStrategy(
    chatClient: client.GetChatClient("gpt-4o-mini").AsIChatClient(),
    trigger: CompactionTriggers.MessagesExceed(8));

var compactionProvider = new CompactionProvider(compactionStrategy: compactionStrategy);

# pragma warning restore MAAI001
agentOptions.AIContextProviders = [compactionProvider];
AIAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId).AsAIAgent(agentOptions);

// Start a new session for the agent conversation.
AgentSession session = await agent.CreateSessionAsync();

//var prompt1 = "What are the top 10 tamil movies according to IMDB?"; // "Ok, can you give the top only"
var prompt1 = "IMDB top 10 tamil movies";

Utils.Gray(prompt1);
var embedding = await embeddingGenerator.GenerateAsync(prompt1);
//var cached = await FindSimilarityAsync(container, embedding.Vector.ToArray());
// Cache lookup
var cached = await CosmosLogging.TraceAsync(
    "SemanticCache.Lookup",
    () => FindSimilarityAsync(cacheContainer.Container, embedding.Vector.ToArray()),
    logger);

if (cached != null)
{
    Console.WriteLine("Cache HIT, lucky you! no money spent on this one:");
    Console.WriteLine(cached.answer);
    return;
}

Console.WriteLine("Cache MISS -> Calling LLM");

// step 2: Call agent /LLM
AgentResponse response = await agent.RunAsync(prompt1, session);

// step 3: store in cache
var item = new SemanticCacheItem
{
    id = Guid.NewGuid().ToString(),
    pk = "cache",
    question = prompt1,
    embedding = embedding.Vector.ToArray(),
    answer = response.Text
};

//await container.Container.CreateItemAsync(item);
// Cache write
await CosmosLogging.TraceAsync(
    "SemanticCache.Write",
    async () =>
    {
        await cacheContainer.Container.CreateItemAsync(item, new PartitionKey(item.pk));
        return item;
    },
    logger);

Console.WriteLine(response.Text);



async Task<SemanticCacheItem?> FindSimilarityAsync(Container container, float[] embedding)
{
    var query = new QueryDefinition(@"
        SELECT TOP 1 c.id,c.question,c.answer, VectorDistance(c.embedding,@embedding) AS similarity
        FROM c
        WHERE c.pk = @pk AND VectorDistance(c.embedding,@embedding) > 0.3
        ORDER BY VectorDistance(c.embedding,@embedding)
")
  .WithParameter("@embedding", embedding)
  .WithParameter("@pk", "cache");

    using var iterator = container.GetItemQueryIterator<SemanticCacheItem>(query);
    while (iterator.HasMoreResults)
    {
        foreach(var item in await iterator.ReadNextAsync())
        {
            return item;
        }
    }
    return null;
}

public class SemanticCacheItem
{
    public required string id { get; set; }
    public string pk { get; set; } = "cache";
    public required string question { get; set; }
    public required float[] embedding { get; set; }
    public required string answer { get; set; }
}

