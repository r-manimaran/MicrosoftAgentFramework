/*
 * 1. Get an Anthropic Api key in (https://docs.claude.com/en/api/admin-api/apikeys/get-api-key)
 * 2. Add Nuget package (Anthropic.SDK + Microsoft.Agents.AI)
 * 3. Create an AnthropicClient for a chatClientAgent
 */

using Anthropic.SDK;
using Anthropic.SDK.Constants;
using ConsoleAppAntropicClaude;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

IChatClient client = new AnthropicClient(new APIAuthentication(ClaudeAIConfig.ApiKey)).Messages.AsBuilder().Build();

ChatClientAgentRunOptions runOptions = new ChatClientAgentRunOptions(new ChatOptions()
{
    ModelId = AnthropicModels.Claude35Haiku,
    MaxOutputTokens = 1000
});
AIAgent agent = new ChatClientAgent(client);
AgentRunResponse response = await agent.RunAsync("Write a haiku about the sea", options:runOptions);
Console.WriteLine(response);

Console.WriteLine("----------------");
Console.WriteLine("Streaming response:");

await foreach(AgentRunResponseUpdate update in agent.RunStreamingAsync("How to make a vegetable biriyani in south indian style?", options: runOptions))
{
    Console.Write(update);
}

Console.ReadKey();