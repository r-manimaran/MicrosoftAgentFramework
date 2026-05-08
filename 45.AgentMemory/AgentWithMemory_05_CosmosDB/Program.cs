

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using Shared;

var client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
                new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));

// --------------------
// Declare the Cosmos Client
// -----------------------
var cosmosClient = new CosmosClient(LLMConfig.AzureCosmosEndpoint, LLMConfig.AzureCosmosKey);
var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(LLMConfig.AzureCosmosDB);

var containerProperties = new ContainerProperties(
    id: LLMConfig.AzureCosmosContainer,
    partitionKeyPaths: new List<string> { "/tenantId", "/userId", "/conversationId" }
);

var container = await database.Database.CreateContainerIfNotExistsAsync(containerProperties);

// ----------------
// History provider
// ----------------
string conversationId = "AgentConversation";

var historyProvider = new CosmosChatHistoryProvider(
    cosmosClient: cosmosClient,
    databaseId: database.Database.Id,
    containerId: container.Resource.Id,
    stateInitializer: _ => new CosmosChatHistoryProvider.State(
        conversationId:conversationId,
        tenantId: "default-tenant",
        userId:"user1"),
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

//var prompt1 = "What are the top 10 tamil movies according to IMDB?"; // "Ok, can you give the top 5 only"

var prompt1 = "Ok, can you give the top 5 only";
Utils.Gray(prompt1);
AgentResponse response = await agent.RunAsync(prompt1, session);
Console.WriteLine(response);