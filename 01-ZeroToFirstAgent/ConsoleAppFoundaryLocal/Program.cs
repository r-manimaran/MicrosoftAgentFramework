
using Microsoft.Agents.AI;
using OpenAI;
using System.ClientModel;

OpenAIClient client = new OpenAIClient(new ApiKeyCredential("NO_API_KEY"), new OpenAIClientOptions
{
    Endpoint = new Uri(" http://localhost:5273")
});
AIAgent agent = client.GetChatClient("qwen2.5-1.5b-instruct-generic-gpu").CreateAIAgent();
AgentRunResponse response = await agent.RunAsync("Write a haiku about the sea");
Console.WriteLine(response);

Console.WriteLine("----------------");
Console.WriteLine("Streaming response:");
await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync("How to make a vegetable biriyani in south indian style?"))
{
    Console.Write(update);
}