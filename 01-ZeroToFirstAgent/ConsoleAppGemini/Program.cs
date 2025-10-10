using ConsoleAppGemini;
using GenerativeAI;
using GenerativeAI.Microsoft;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

IChatClient client = new GenerativeAIChatClient(GeminiAIConfig.ApiKey, GoogleAIModels.Gemini25Flash);
AIAgent agent = new ChatClientAgent(client);

AgentRunResponse response = await agent.RunAsync("Write a poem about the sea in the style of Shakespeare.");
Console.WriteLine(response);

Console.WriteLine("---------------");
Console.WriteLine("Streaming response:");
await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync("Write a short story for the kids about 10 lines"))
{
    Console.Write(update);
}
Console.Read();