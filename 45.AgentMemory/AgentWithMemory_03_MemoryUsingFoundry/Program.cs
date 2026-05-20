using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;
using System.Text;

var projectEndpoint = LLMConfig.FoundryProjectEndpoint;
var modelDeploymentName = "gpt-4o-mini";

Utils.WriteLine("------------------------", ConsoleColor.White);
Utils.WriteLine("Starting Application with Foundry Memory", ConsoleColor.Cyan);
Utils.WriteLine("------------------------", ConsoleColor.White);


//AIProjectClient projectClient = new AIProjectClient(new Uri(projectEndpoint),
//                                                        new DefaultAzureCredential());

var memoryStoreName = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_MEMORY_STORE_NAME") ?? "joker-memory-store";
var chatModel = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_MODEL") ?? "gpt-4o-mini";
var embeddingModel = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_EMBEDDING_MODEL") ?? "text-embedding-3-small";

//await FoundryMemoryProvider.EnsureMemoryStoreCreateAsync(
//    projectClient,
//    memoryStoreName,
//    chatModel: chatModel,
//    embeddingModel: embeddingModel);
#pragma warning disable MAAI001
// MAAI001: Ensure the memory store exists befo
//var foundryMemoryProvider = new FoundryMemoryProvider(
//    projectClient,
//    memoryStoreName,
//    options: new FoundryMemoryProviderOptions
//    {
//        MaxMemories = 5,           // max memories injected per turn (default: 5)
//        UpdateDelay = 5,           // seconds of inactivity before writing memories
//        ContextPrompt =             // optional: customise the injected memory header
//            "The following are things you already know about this user:"
//    });
#pragma warning enable MAAI001

// ── 3. Build the FoundryMemoryProvider ────────────────────────────────────────
// Scope controls memory isolation. "UID1" keeps memories per-user across sessions.
// Equivalent to your original: searchScope: new() { UserId = "UID1" }
//var foundryMemoryProvider = new FoundryMemoryProvider(
//    projectClient,
//    memoryStoreName,
//    scope: "UID1",                  // per-user isolation (multi-tenant safe)
//    options: new FoundryMemoryProviderOptions
//    {
//        MaxMemories = 5,           // max memories injected per turn (default: 5)
//        UpdateDelay = 5,           // seconds of inactivity before writing memories
//        ContextPrompt =             // optional: customise the injected memory header
//            "The following are things you already know about this user:"
//    });


var client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new ApiKeyCredential(LLMConfig.ApiKey));
    
AIAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "Joker",
        ChatOptions = new() { Instructions = "You are good at telling jokes." },
        AIContextProviders = []
    }).AsBuilder()
    .Use(FunctionCallingMiddleware).Build();

AgentSession session1 = await agent.CreateSessionAsync();

var prompt1 = "I like jokes about pirates. Tell me a joke about pirates.";
Utils.Gray(prompt1);
Console.WriteLine(await agent.RunAsync(prompt1, session1));


AgentSession session2 = await agent.CreateSessionAsync();

var prompt2 = "Tell me a joke that I might like.";
Utils.Gray(prompt2);
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


