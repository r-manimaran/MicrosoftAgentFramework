using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using McpAgentApp;
using McpAgentApp.Extensions;
using McpAgentApp.Gateway;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI.Chat;
using System.ClientModel;
using System.Text;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using HttpClientTransport = ModelContextProtocol.Client.HttpClientTransport;

Utils.WriteLineInformation("Starting Agent with MCP tools...");
Utils.Separator();
AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
    new ApiKeyCredential(LLMConfig.ApiKey));

IChatClient chatClient = client.GetChatClient(LLMConfig.DeploymentOrModelId).AsIChatClient();

Task.Delay(1000).Wait();
// Create the MCP Client by calling directly to the Server.
/*
McpClient mcpClient = await McpClient.CreateAsync(new HttpClientTransport(
    new HttpClientTransportOptions
    {
        Endpoint = new Uri(LLMConfig.McpEndpoint),
        TransportMode = HttpTransportMode.StreamableHttp,

    }
    //httpClient,
    //ownsHttpClient: false
    ));

 IList<McpClientTool> toolsInMcp = await mcpClient.ListToolsAsync();
 */

await using GatewayMcpClient mcpClient = new GatewayMcpClient(
    gatewayBaseUrl: LLMConfig.McpGatewayBaseUrl,
    agentId: LLMConfig.McpAgentId,
    serverId: LLMConfig.McpServerId);

IList<GatewayTool> toolsInMcp = await mcpClient.ListToolsAsync();

Utils.WriteLine("Tools in MCP via gateway:", ConsoleColor.Cyan);
Utils.WriteLineWarning("------------------------------------");

foreach (var tool in toolsInMcp)
{
    Utils.WriteLineSuccess($"- {tool.Name}");
}

// Convert gateway tools to AIFunctions — same as McpClientTool.AsAIFunction() in M2
IList<AITool> aiTools = toolsInMcp
    .Select(t => t.AsAIFunction())
    .Cast<AITool>()
    .ToList();

// ─── Agent conversation loop ──────────────────────────────────────────────────
var messages = new List<ChatMessage>
{
    new(ChatRole.System, """
        You are an IT support assistant. You have access to tools for managing
        support tickets. Use the tools to answer questions accurately — always
        query for current data rather than guessing. When you find relevant
        information, present it clearly and suggest next steps.
        """)
};

// -- Create the AIAgent

/* AIAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .AsAIAgent(
    instructions: "You are a helpful assistant that can use tools to answer questions.",
    tools: toolsInMcp.Cast<AITool>().ToList()
    ).AsBuilder()
    .Use(FunctionCallingMiddleware)
    .Build(); */

Utils.WriteLine("\nIT Support Agent (via MCP Gateway) — type 'exit' to quit", ConsoleColor.Cyan);
Utils.WriteLineWarning("Gateway: " + LLMConfig.McpGatewayBaseUrl);
Utils.WriteLineWarning("Server:  " + LLMConfig.McpServerId);
Utils.WriteLineWarning("------------------------");

while (true)
{
    Console.Write("User:>");
    string userInput = Console.ReadLine() ?? string.Empty;
    if (string.IsNullOrEmpty(userInput) || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
    messages.Add(new ChatMessage(ChatRole.User, userInput));

    while (true)
    {
        var options = new ChatOptions { Tools = aiTools };
        var response = await chatClient.GetResponseAsync(messages, options);

        var toolCalls = response.Messages
                .SelectMany(m => m.Contents.OfType<FunctionCallContent>())
                .ToList();

        if (toolCalls.Count == 0)
        {
            // No more tool calls - print final answer
            Utils.WriteLine($"\nAssitant:> {response.Text}", ConsoleColor.White);
            messages.AddRange(response.Messages);
            break;
        }
        messages.AddRange(response.Messages);

        // Execute tool calls through the gateway
        var toolResults = new List<ChatMessage>();
        foreach (var call in toolCalls)
        {
            Utils.WriteLine($"\n[Gateway] Calling {call.Name}...", ConsoleColor.DarkGray);

            try
            {
                var tool = toolsInMcp.FirstOrDefault(t => t.Name == call.Name);
                if (tool is null)
                {
                    toolResults.Add(new ChatMessage(ChatRole.Tool,
                        $"Tool '{call.Name}' not found"));
                    continue;
                }

                var toolArgs = call.Arguments as IReadOnlyDictionary<string,object>
                            ?? new Dictionary<string, object?>();
                Utils.WriteLine(
                     $"[Debug] {call.Name} args: {System.Text.Json.JsonSerializer.Serialize(call.Arguments)}",
                        ConsoleColor.DarkGray);
                var result = await tool.InvokeAsync(toolArgs);

                Utils.WriteLine($"[Gateway] {call.Name} → {result[..Math.Min(80, result.Length)]}...",
                   ConsoleColor.DarkGreen);

                toolResults.Add(new ChatMessage(ChatRole.Tool,
                  [new FunctionResultContent(call.CallId ?? "", result)]));
            }
            catch (Exception ex)
            {
                Utils.WriteLineError($"Tool call failed: {ex.Message}");
                toolResults.Add(new ChatMessage(ChatRole.Tool, $"Error calling tool '{call.Name}': {ex.Message}"));
            }
        }

        //response.Usage.OutputAsInformation();
        //messages.AddRange(response.Messages);
        messages.AddRange(toolResults);
    }
}
Utils.WriteLine("\nGoodbye!", ConsoleColor.Cyan);

// -- Middleware
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