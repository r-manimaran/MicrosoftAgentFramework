using A2A;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Shared;
using OpenAI.Chat;

Console.Clear();
Utils.WriteLine("Initializing", ConsoleColor.DarkGray);
Utils.WriteLine("Waiting 1 sec for the server to be ready", ConsoleColor.DarkGray);
await Task.Delay(1000);

var secrets = SecretsManager.GetAzureOpenAICredentials();
Utils.WriteLine("- Connecting to Remote Agent", ConsoleColor.DarkGray);
A2ACardResolver agentCardResolver = new A2ACardResolver(new Uri("http://localhost:5000/"));
AIAgent remoteAgent = await agentCardResolver.GetAIAgentAsync();

Utils.Separator();
Utils.WriteLine("Initializing", ConsoleColor.DarkGray);
AzureOpenAIClient client = new(secrets.endpoint, secrets.apiKey);
ChatClientAgent agent = client.GetChatClient("gpt-4o-mini")
    .AsAIAgent(
      name: "ClientAgent",
      instructions: "You are specialize in handling queries for users and using your tools to provide answers.",
      tools: [remoteAgent.AsAIFunction()]);
    
AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string message = Console.ReadLine() ?? string.Empty;
    if (message == string.Empty) continue;

    AgentResponse response = await agent.RunAsync(message, session);
    Utils.WriteLine(response.ToString(), ConsoleColor.Magenta);
}