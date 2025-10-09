using ConsoleApp;
using Microsoft.Agents.AI;
using OpenAI;
using Azure.AI.OpenAI;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(AIConfig.Endpoint),
                                               new Azure.AzureKeyCredential(AIConfig.ApiKey));

AIAgent agent = client.GetChatClient(AIConfig.DeploymentOrModelId).CreateAIAgent();

// Simple Answer
//AgentRunResponse response = await agent.RunAsync("What is the capital of France?");
//Console.WriteLine(response);

// Streaming Answer
await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync("Tell me a story with a twist and turns?"))
{
    
    Console.Write(update);
    
}

Console.ReadLine();
