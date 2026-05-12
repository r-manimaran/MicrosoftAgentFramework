using AgentApp;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;


var client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
                new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey))
    .GetChatClient(LLMConfig.DeploymentOrModelId);


IChatClient chatClient = client.AsIChatClient();

AIAgent dotNetAgent = GetDotNetAgent(chatClient);

var dotNetAgentExecutor = new AgentExecutor("DotNetAgentExecutor", dotNetAgent);
var displayExecutor = new DisplayExecutor();

var workflow = new WorkflowBuilder(dotNetAgentExecutor)
    .AddEdge(dotNetAgentExecutor, displayExecutor).WithOutputFrom(displayExecutor).Build();

string prompt = "Write a Razor page that shows Hello World";

await using StreamingRun sr = await InProcessExecution.RunStreamingAsync(workflow, input: prompt);
await foreach (WorkflowEvent evnt in sr.WatchStreamAsync())
{
    if(evnt is AIAgentResponseEvent ere)
    {
        Console.WriteLine(ere);
    }
}
static ChatClientAgent GetDotNetAgent(IChatClient chatClient)
{
    return new ChatClientAgent(chatClient, new ChatClientAgentOptions()
    {
        Id = "1",
        Description = "Writes a .NET C# Razor web pages",
        Name = "DotNetAgent",
        ChatOptions = new ChatOptions()
        {
            Instructions = "You are a .NET Software developer specialises in ASP.NET 10 and C#"
        }
    });
}