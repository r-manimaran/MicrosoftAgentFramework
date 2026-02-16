using Anthropic;
using Anthropic.Core;
using Anthropic_Reasoning;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

string question = "What is the capital of the state where the Marina beach is located" +
                   " and what is the population of that city? (Answer back in max 3 words)";

Console.Clear();
Utils.WriteLineSuccess("Baseline (No thiniking)");
await Baseline();

Console.ReadLine();



async Task Baseline()
{
    AnthropicClient client = new(new ClientOptions
    {
        ApiKey = LLMConfig.ApiKey
    });
    ChatClientAgent agent = new(client.AsIChatClient("claude-haiku-4-5-20251001"),
        new ChatClientAgentOptions
        {
           ChatOptions = new ChatOptions
           {
               MaxOutputTokens = 10000
           }            
        });
    AgentResponse response = await agent.RunAsync(question);
    Console.WriteLine(response);
    response?.Usage.OutputAsInformation();
}