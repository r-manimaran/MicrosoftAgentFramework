

// Create a vector store to store overflow chat messages
// using here In-memort vector store
using AgentWithMemory_04_BoundedChatHistory;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OpenAI.Chat;
using Shared;

VectorStore vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions()
{
    EmbeddingGenerator = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
                        new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey))
                        .GetEmbeddingClient(LLMConfig.EmbeddingDeploymentName)
                        .AsIEmbeddingGenerator()
});

var sessionId = Guid.NewGuid().ToString();

// Create BounderdChatHistoryProvider with a maximum of 4 non-system messages in session state.
// It internally creates an InMemoryChatHistoryProvider with a TruncatingChatReducer and a
// ChatHistoryMemoryProvider with the correct configuration to ensure overflow messages are
// automatically archived to the vector store and recalled via semantic search.
var boundedProvider = new BoundedChatHistoryProvider(
    maxSessionMessages: 4,
    vectorStore,
    collectionName: "chathistory-overflow",
    vectorDimensions: 3072,
    session => new ChatHistoryMemoryProvider.State(
        storageScope: new() { UserId = "UID1", SessionId = sessionId },
        searchScope: new() { UserId = "UID1" }));

// Create the Agent
AIAgent agent = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
    new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey))
    .GetChatClient(LLMConfig.DeploymentOrModelId)
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new() { Instructions = "You are a helpful assistant. Answer questions concisely." },
        Name = "Assistant",
        ChatHistoryProvider = boundedProvider
    });

// Start a Conversation
AgentSession session = await agent.CreateSessionAsync();
Console.WriteLine("--- Filling the session window (4 messages max) ---\n");
Console.WriteLine(await agent.RunAsync("My favorite color is blue.", session));
Console.WriteLine(await agent.RunAsync("I have a dog named Max.", session));
// At this point the session state holds 4 messages (2 user + 2 assistant).
// The next exchange will push the oldest messages into the vector store.
Console.WriteLine("\n--- Next exchange will trigger overflow to vector store ---\n");
Console.WriteLine(await agent.RunAsync("What is the capital of France?", session));
// The oldest messages about favorite color have now been archived to the vector store.
// Ask the agent something that requires recalling the overflowed information.
Console.WriteLine("\n--- Asking about overflowed information (should recall from vector store) ---\n");

Console.WriteLine(await agent.RunAsync("What is my favorite color?", session));