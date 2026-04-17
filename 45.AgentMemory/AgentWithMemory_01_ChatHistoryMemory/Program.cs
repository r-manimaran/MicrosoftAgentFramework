

// Create a vector store to store the chat message in.
// For prototype, we can use In-Memory and for prod scenario we can go with any vector db provider.

using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OpenAI.Chat;
using Shared;
using System.Text;

Utils.WriteLine("------------------------", ConsoleColor.White);
Utils.WriteLine("Starting Application with InMemory", ConsoleColor.Cyan);
Utils.WriteLine("------------------------", ConsoleColor.White);

VectorStore vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions()
{
    EmbeddingGenerator = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
                        new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey))
                            .GetEmbeddingClient("text-embedding-3-small").AsIEmbeddingGenerator()

});

// Create the agent and add the ChatHistoryMemoryProvider to store chat messages inthe vector store
AIAgent agent = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
                new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey))
    .GetChatClient(LLMConfig.DeploymentOrModelId)
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new() { Instructions = "You are good at telling jokes." },
        Name = "Joker",
        AIContextProviders = [
            new ChatHistoryMemoryProvider(
                vectorStore,
                collectionName: "chatHistory",
                vectorDimensions:3072,
                // Callback to configure the initial state of the ChatHistoryMemoryProvider.
                // The ChatHistoryMemoryProvider stores its state in the AgentSession and this callback
                // will be called whenever the ChatHistoryMemoryProvider cannot find existing state in the session,
                // typically the first time it is used with a new session.
                session => new ChatHistoryMemoryProvider.State(
                   // configure the scope values under which messages will be stored. 
                   // In this case, we are using a fixed user ID and a unique session ID for each new sesion.
                   storageScope: new() { UserId="UID1", SessionId=Guid.NewGuid().ToString()},
                   //configure the scope which woluld be used to search for relevant prioir messages.
                   // In this case, we are searchng for any messages for the user accros all sessions.
                   searchScope: new() { UserId ="UID1"}))
            ]
    }).AsBuilder()
    .Use(FunctionCallingMiddleware).Build();

// Start a new session for the agent conversation.
AgentSession session = await agent.CreateSessionAsync();

var prompt1 = "I like jokes about Pirates. Tell me a joke about a pirate.";
Utils.Gray(prompt1);
// Run the agent with the session that stores conversation history in the vector store.
Console.WriteLine(await agent.RunAsync(prompt1, session));

// Start the second session, Since we configured the search scope to be accross all sessions for the user,
// the agent should remember that the user likes pirate jokes.
AgentSession? session2 = await agent.CreateSessionAsync();

var prompt2 = "Tell me a joke that I might like.";
Utils.Gray(prompt2);
// Run the agent with the second session
Console.WriteLine(await agent.RunAsync(prompt2, session2));



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

