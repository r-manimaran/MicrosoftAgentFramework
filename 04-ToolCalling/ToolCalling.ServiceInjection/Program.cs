

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using ToolCalling.ServiceInjection;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new Azure.AzureKeyCredential(LLMConfig.ApiKey));

ServiceCollection services = new();
services.AddScoped<HttpClient>();
services.AddScoped<ToolClass1>();
services.AddScoped<ToolClass2>();
IServiceProvider serviceProvider = services.BuildServiceProvider();

ToolClass1 toolClass1Instance = serviceProvider.GetRequiredService<ToolClass1>();
ToolClass2 toolClass2Instance = serviceProvider.GetRequiredService<ToolClass2>();

ChatClientAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .CreateAIAgent(
        instructions: "You are a helpful assistant. Use the tools to respond to the user queries.",
        tools: new List<AITool>
        {
            AIFunctionFactory.Create(StaticTool, "tool1"),
            AIFunctionFactory.Create(toolClass1Instance.ToolInToolClass, "tool2"),
            AIFunctionFactory.Create(ToolClass2.ToolInToolClass,"tool3"),
            AIFunctionFactory.Create(toolClass2Instance.ToolInToolClassInstance,"tool4")
        },
        services: serviceProvider // Inject the service provider
    );

AgentRunResponse response = await agent.RunAsync("Call Tool1");
Console.WriteLine($"Response from Tool1: {response}");

response = await agent.RunAsync("Call Tool2");
Console.WriteLine($"Response from Tool2: {response}");

response = await agent.RunAsync("Call Tool3");
Console.WriteLine($"Response from Tool3:{response}");
#region Agents

#endregion




static string StaticTool()
{
    return "Say 'I'm a static tool to the user";
}

class ToolClass1(HttpClient httpClient)
{
    public string ToolInToolClass()
    {
        return "I'm an instance tool to the user";
    }
}

class ToolClass2
{
    public static string ToolInToolClass(IServiceProvider serviceProvider)
    {
        var httpClient = serviceProvider.GetRequiredService<HttpClient>();
        return "I'm a static tool in a tool class to the user";
    }

    public string ToolInToolClassInstance(IServiceProvider serviceProvider)
    {
        var httpClient = serviceProvider.GetRequiredService<HttpClient>();
        return "I'm an instance tool in a tool class to the user";
    }
}