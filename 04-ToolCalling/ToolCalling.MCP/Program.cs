using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using System.ClientModel;
using System.Text;
using ToolCalling.MCP;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new ApiKeyCredential(LLMConfig.ApiKey));

// Create the MCP client with the GitHub Copilot API endpoint and authentication
McpClient githubMcpClient = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
{
    TransportMode = HttpTransportMode.StreamableHttp,
    Endpoint = new Uri("https://api.githubcopilot.com/mcp/"),
    AdditionalHeaders = new Dictionary<string, string>
    {
        { "Authorization", LLMConfig.GitHubToken }
    }
}));

// Create the MCP client with the Google Maps API MCP endpoint and authentication
McpClient mapsMcpClient = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
{
    TransportMode = HttpTransportMode.StreamableHttp,
    Endpoint = new Uri("https://mapstools.googleapis.com/mcp"),
    AdditionalHeaders = new Dictionary<string, string>
    {
        {"X-Goog-Api-Key",LLMConfig.GoogleMapsApiKey }
    }
}));


IList<McpClientTool> toolInGitHubMcp = await githubMcpClient.ListToolsAsync();
IList<McpClientTool> toolInGoogleMapsMcp = await mapsMcpClient.ListToolsAsync();

AIAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .CreateAIAgent(
        instructions: " You are a GitHub Expert and Google Maps Expert. Analyze the user query and use appropriate tools from the MCP",
        tools: toolInGitHubMcp.Cast<AITool>().Concat(toolInGoogleMapsMcp.Cast<AITool>()).ToList()            
    ).AsBuilder()
    .Use(FunctionCallingMiddleware) // Enable function calling middleware
    .Build();

/*AIAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .CreateAIAgent(
        instructions: " You are a Google Map Expert",
        tools: toolInGoogleMapsMcp.Cast<AITool>().ToList()
    ).AsBuilder()
    .Use(FunctionCallingMiddleware) // Enable function calling middleware
    .Build();*/

AgentThread thread = agent.GetNewThread();

while (true)
{
    Console.Write("User: >");
    string userInput = Console.ReadLine() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(userInput))
    {
        break;
    }
    ChatMessage userMessage = new ChatMessage(ChatRole.User, userInput);
    AgentRunResponse response = await  agent.RunAsync(userMessage, thread);
      
    Console.WriteLine($"Agent: > {response}");
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