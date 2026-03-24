
// - Build the base agent ( No tools registered upfront)
using Azure.AI.OpenAI;
using ECommerceAgent;
using ECommerceAgent.Middleware;
using Microsoft.Agents.AI;
using OpenAI.Chat;

var client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));

var baseAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .AsAIAgent(instructions: """
    You are a friendly e-commerce customer support agent.
    Help customers with their orders, returns, payments, and shipping.
    Always be concise and empathetic. Ask for an order ID if you need one.
    """);

// - Wrap with dynamic tool injection middleware
var agent = baseAgent.AsBuilder()
                .Use(
                    runFunc: DynamicToolMiddleware.InjectToolsAsync,
                    runStreamingFunc: DynamicToolMiddleware.InjectToolsStreamingAsync
                )
                .Build();

// ── Start a session (maintains conversation history across turns) ─────────────
AgentSession session = await agent.CreateSessionAsync();

// ── Conversation loop ─────────────────────────────────────────────────────────
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("=== E-Commerce Support Agent (Dynamic Tools) ===");
Console.WriteLine("Type 'exit' to quit.\n");
Console.ResetColor();

while (true)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("You: ");
    Console.ResetColor();

    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    var response = await agent.RunAsync(input, session);

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("Agent: ");
    Console.ResetColor();
    Console.WriteLine(response.Text);
    Console.WriteLine();
}