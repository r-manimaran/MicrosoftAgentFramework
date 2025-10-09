using ConsoleAppOpenAI;
using Microsoft.Agents.AI;
using OpenAI;

OpenAIClient client = new OpenAIClient(AIConfig.ApiKey);
AIAgent agent =client.GetChatClient(AIConfig.ModelId).CreateAIAgent();

AgentRunResponse response = await agent.RunAsync("What is the capital of India?");
Console.WriteLine(response);


//Streaming Response
await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync("How to make a soup with vegetables?"))
{
    Console.Write(update);
}

Console.ReadLine();