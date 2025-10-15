using AgentApp;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using OpenAI;
using SharedLib;
using SharedLib.Extensions;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new Azure.AzureKeyCredential(LLMConfig.ApiKey));

AIAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId).CreateAIAgent();
string userMessage = "What is the capital of India?";

AgentRunResponse response = await agent.RunAsync(userMessage);

Utils.WriteLineInformation($"User Message: {userMessage}");
Console.WriteLine(response);
Utils.WriteLineInformation($"- Input Tokens: {response.Usage?.InputTokenCount}");
Utils.WriteLineInformation($"- Output Tokens: {response.Usage?.OutputTokenCount} "+
    $"({response.Usage?.GetOutputTokensUsedForReasoning()} was used for reasoning)");
Utils.Separator();

// Streaming Response
Utils.WriteLineInformation("Streaming response:");
string streamingUserMessage = "How to make vegetable biriyani in south indian style?";
Utils.WriteLineInformation($"User Message: {streamingUserMessage}");
List<AgentRunResponseUpdate> updates = new List<AgentRunResponseUpdate>();

await foreach(AgentRunResponseUpdate update in agent.RunStreamingAsync(streamingUserMessage))
{
    updates.Add(update);
    Console.Write(update);
}
AgentRunResponse collectedResponseFromStreaming = updates.ToAgentRunResponse();
Utils.WriteLineInformation($"- Input Tokens: {collectedResponseFromStreaming.Usage?.InputTokenCount}");
Utils.WriteLineInformation($"- Output Tokens: {collectedResponseFromStreaming.Usage?.OutputTokenCount} " +
    $"({collectedResponseFromStreaming.Usage?.GetOutputTokensUsedForReasoning()} was used for reasoning)");
Utils.Separator();

Console.ReadLine();