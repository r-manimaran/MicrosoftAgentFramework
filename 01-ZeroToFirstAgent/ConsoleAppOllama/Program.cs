/*
 * Download Ollama + Model (https://ollama.com/)
 * Add Nuget package (OllamaSharp + Microsoft.Agents.AI)
 * Create an OllamaClient for a chatClientAgent
 */

using ConsoleAppOllama;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

IChatClient client = new OllamaApiClient(OllamaConfig.Endpoint, OllamaConfig.Model);
// AIAgent agent = new ChatClientAgent(client);
AIAgent agent = client.CreateAIAgent();
AgentRunResponse response = await agent.RunAsync("Write a haiku about the sea");
Console.WriteLine(response);

Console.WriteLine("----------------");
Console.WriteLine("Streaming response:");
await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync("How to make a vegetable biriyani in south indian style?"))
{
    Console.Write(update);
}

DataContent imgDc = new DataContent(File.ReadAllBytes("sea.png"), "image/png");
ChatMessage message = new ChatMessage(ChatRole.User, [new TextContent("Describe the image"), imgDc]);
AgentRunResponse imgResponse = await agent.RunAsync(message);
Console.WriteLine(imgResponse);