using ConsoleAppXAiGrok;
using Microsoft.Agents.AI;
using OpenAI;
using System.ClientModel;

OpenAIClient client = new OpenAIClient(new ApiKeyCredential(GroqAIConfig.ApiKey), new OpenAIClientOptions
{
    Endpoint = new Uri(GroqAIConfig.Endpoint)
});

AIAgent agent = client.GetChatClient(GroqAIConfig.ModelId).CreateAIAgent();

AgentRunResponse response = await agent.RunAsync("Write a poem about the sea in the style of Shakespeare.");
Console.WriteLine(response);

Console.WriteLine("----------------------");
Console.WriteLine("Streaming response:");
await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync("Write a poem about the sea in the style of Shakespeare."))
{
    Console.Write(update);
}

Console.ReadKey();
