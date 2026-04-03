using AgentAppMcpConnect;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI.Chat;
using System.ClientModel;
using System.Text;

Utils.WriteLineInformation("Starting Agent with MCP tools...");
Utils.Separator();
AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
    new ApiKeyCredential(LLMConfig.ApiKey));

// Create the named HttpClient 
HttpClient httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("X-Api-Key", LLMConfig.McpApiKey);
Utils.WriteLine($"Using Api Key Authentication for MCP Client : {LLMConfig.McpApiKey}", ConsoleColor.Cyan);

// Create the MCP Client
McpClient apiKeyProtectedMcpClient = await McpClient.CreateAsync(new HttpClientTransport(
    new HttpClientTransportOptions
    {
        Endpoint = new Uri(LLMConfig.McpEndpoint),
        TransportMode = HttpTransportMode.StreamableHttp,

    },
    httpClient,
    ownsHttpClient: false));

IList<McpClientTool> toolsInMcp = await apiKeyProtectedMcpClient.ListToolsAsync();
Utils.WriteLine("Tools in MCP:", ConsoleColor.Cyan);
Utils.WriteLineWarning("------------------------");
foreach (var tool in toolsInMcp)
{
    Utils.WriteLineSuccess($"- {tool.Name}");
}

AIAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .AsAIAgent(
    instructions: "You are a helpful assistant that can use tools to answer questions.",
    tools: toolsInMcp.Cast<AITool>().ToList()
    ).AsBuilder()
    .Use(FunctionCallingMiddleware)
    .Build();

while (true)
{
    Console.Write("User:>");
    string userInput = Console.ReadLine() ?? string.Empty;
    if (string.IsNullOrEmpty(userInput))
    {
        break;
    }
    var userMessage = new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userInput);
    AgentResponse response = await agent.RunAsync(userMessage);
    Console.WriteLine($"Agent:>{response}");
    Utils.Separator();
}

async ValueTask<object?> FunctionCallingMiddleware(AIAgent callingAgent, FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken token)
{
    StringBuilder functionCallDetails = new();
    functionCallDetails.Append($"-Tool Call: '{context.Function.Name}'");
    if (context.Arguments != null && context.Arguments.Count > 0)
    {
        functionCallDetails.AppendLine();
        functionCallDetails.AppendLine("-With Arguments:");
        functionCallDetails.AppendLine(string.Join(Environment.NewLine, context.Arguments.Select(kv => $"  - {kv.Key}: {kv.Value}")));
    }
    Utils.WriteLineInformation(functionCallDetails.ToString());
    try
    {
        return await next(context, token);
    }
    catch (Exception ex)
    {
        Utils.WriteLineError($"Tool call failed: {ex.Message}");
        throw;
    }
}