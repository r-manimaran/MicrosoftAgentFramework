
using AgentMCPClient;
using AgentMCPClient.Auth;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI.Chat;
using HttpClientTransport = ModelContextProtocol.Client.HttpClientTransport;

Utils.Init("======MCP Client Agent========");

// -- 1. Authenticate with KeyCloak
var http = new HttpClient();
var flow = new DeviceAuthFlow(http, Config.KeycloakBase);
var token = await flow.AcquireTokenAsync();
Utils.Green("Successfully Authenticated with KeyCloak ");

// -- 2. Conntect to Keycloak protected MCP Server
var authedHttp = new HttpClient();
authedHttp.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

var transport = new HttpClientTransport(new HttpClientTransportOptions
{
    Endpoint = new Uri($"{Config.McpServerUrl}/mcp"),
    Name = "McpKeycloakServer",
    TransportMode = HttpTransportMode.StreamableHttp,
    AdditionalHeaders = new System.Collections.Generic.Dictionary<string, string>
    {
        ["Authorization"] = $"Bearer {token}"
    }
});


await using var mcpClient = await McpClient.CreateAsync(transport, new McpClientOptions
{
    ClientInfo = new() 
    {
        Name="MCP Client Agent", 
        Version="1.0.0",
        Description= "An example of an MCP Client Agent that connects to a Keycloak protected MCP Server",
        Title= "MCP Client Agent",
        //Icons = new[] { new McpClientInfoIcon { Url = "https://raw.githubusercontent.com/Azure/ModelContextProtocol/main/docs/images/mcp.png", Type = "image/png" } }

    }
    
});

// -- 3. Get the MCP Tools

var mcpTools = await mcpClient.ListToolsAsync();
Utils.Green($"Successfully connected to MCP Server. Found {mcpTools.Count} tools:");
foreach(var tool in mcpTools)
{
    Utils.Gray($"- {tool.Name} ({tool.Description})");
}
Utils.Separator();


// --4. Create AI Agent with the MCP Tools
AIAgent agent = new AzureOpenAIClient(new Uri(Config.Endpoint), new System.ClientModel.ApiKeyCredential(Config.ApiKey))
    .GetChatClient(Config.DeploymentOrModelId)
    .AsAIAgent(
        instructions: """
                You are a helpful assistant that can use the following tools to answer questions 
                 and complete tasks. Always try to use the tools when appropriate.
                """,
        name: "MCPAssistant",
        description: "Assistant backed by Keycloak-authenticated MCP tools",
        tools: [.. mcpTools.Cast<AITool>()])
    .AsBuilder()
    .Use(Utils.ToolCallingMiddleware)
    .Build();

// --5. Run the conversation Loop.
Utils.WriteLine("Agent ready! You can start asking questions. Type 'exit' to quit.", ConsoleColor.Magenta);

while (true)
{
    Console.Write("You: ", ConsoleColor.Cyan);
    var input = Console.ReadLine();
    if (input.ToLower() == "exit")
        break;

    var response = await agent.RunAsync(input);
    response.Usage.OutputAsInformation();
    Utils.WriteLine($"Agent: {response}", ConsoleColor.Green);
}
    

