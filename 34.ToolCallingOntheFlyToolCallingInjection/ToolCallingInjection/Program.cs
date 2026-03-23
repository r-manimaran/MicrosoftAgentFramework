using AgentFrameworkToolkit.Tools.Common;
using Azure.AI.OpenAI;
using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.Text;
using ToolCallingInjection;
using ToolCallingInjection.Extensions;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

Console.WriteLine("Normal (N) are Inject Mode (I)?:");
var key = Console.ReadKey();
Console.Clear();

switch (key.Key)
{
    case ConsoleKey.N:
        await NormalAgentWithTools();
        break;
    case ConsoleKey.I:
        await ToolInjection();
        break;
    default:
        Console.WriteLine("Invalid choice");
        break;
}

async Task NormalAgentWithTools()
{
    List<AITool> tools = [];
    tools.AddRange(TimeTools.All());
    tools.AddRange(FileSystemTools.All(new FileSystemToolsOptions
    {
        ConfinedToTheseFolderPaths = [@"C:\Maran\FunctionCallingExample"]
    }));
    tools.AddRange(WeatherTools.All(new OpenWeatherMapOptions
    {
        ApiKey = LLMConfig.OpenWeatherApiKey,
        PreferredUnits = WeatherOptionsUnits.Metric
    }));

    AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));
    AIAgent mainAgent = client.GetChatClient("gpt-4o-mini").AsAIAgent(tools: tools).AsBuilder().Use(FunctionCallMiddleware).Build();

    Utils.Green($"This agent have: {tools.Count} tools");
    foreach(var tool in tools)
    {
        Utils.Gray($"- {tool.Name}");
    }

    while (true)
    {
        Console.Write("> ");
        string input = Console.ReadLine() ?? "";
        AgentResponse response = await mainAgent.RunAsync(input);
        Console.WriteLine(response);
        response.Usage.OutputAsInformation();
        Utils.Separator();
    }
}

async Task ToolInjection()
{
    AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new System.ClientModel.ApiKeyCredential (LLMConfig.ApiKey));
    ChatClientAgent toolinjectionAgent = client.GetChatClient("gpt-4o-mini")
                                               .AsAIAgent(
                                    instructions: "Your job is to tell if any given message is a request to use specific tools");

    AIAgent mainAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId).AsAIAgent(new ChatClientAgentOptions
    {
        AIContextProviders = [new OnTheFlyToolInjectionContext(toolinjectionAgent)]
    })
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();

    Utils.WriteLine("This agent have : 0 tools", ConsoleColor.Green);
    
    while (true)
    {
        Console.Write("> ");
        string input = Console.ReadLine() ?? "";
        AgentResponse response = await mainAgent.RunAsync(input);
        Console.WriteLine(response);
        response.Usage.OutputAsInformation();
        Utils.Separator();
    }
}

async ValueTask<object?> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context,
                                    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
                                    CancellationToken cancellationToken = default)
{
    StringBuilder functionCallDetails = new();
    functionCallDetails.Append($" - Tool call :'{context.Function.Name}' [Agent:{callingAgent.Name}]");
    if (context.Arguments.Count > 0)
    {
        functionCallDetails.Append(" with args: ");
        foreach (var arg in context.Arguments)
        {
            functionCallDetails.Append($" {arg.Key}='{arg.Value}' ");
        }
    }
    Utils.WriteLine(functionCallDetails.ToString(),ConsoleColor.DarkGray);
    return await next(context, cancellationToken);
}

class OnTheFlyToolInjectionContext(ChatClientAgent toolInjectionAgent) : AIContextProvider
{
    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        IEnumerable<ChatMessage> messages = context.AIContext.Messages ?? [];

        AgentResponse<ToolResult> response = await toolInjectionAgent.RunAsync<ToolResult>(messages, 
                                                                cancellationToken: cancellationToken);
        List<AITool> injectedTools = [];

        string? injectedInstructions = null;
        
        ToolResult toolResult = response.Result;

        if (toolResult.NeedTimeTools)
        {
            Utils.Green("Time tools injected");
            injectedTools.AddRange(TimeTools.All());
        }

        if (toolResult.NeedFileSystemTools)
        {
            Utils.Green("File System Tools injected");
            injectedTools.AddRange(FileSystemTools.All(new FileSystemToolsOptions
            {
                ConfinedToTheseFolderPaths = ["C:\\TestAI"]
            }));
            injectedInstructions = "When working with files your root folder is 'C:\\TestAI'";
        }

        if (toolResult.NeedWeatherTools)
        {
            Utils.Green("Weather Tools injected");
            injectedTools.AddRange(WeatherTools.All(new OpenWeatherMapOptions
            {
                ApiKey = LLMConfig.OpenWeatherApiKey,
                PreferredUnits = WeatherOptionsUnits.Metric
            }));
        }

        Utils.Green($"Number of tool's injected: {injectedTools.Count}");

        return new AIContext
        {
            Instructions = injectedInstructions,
            Tools = injectedTools
        };
    }
    
    [PublicAPI]
    private class ToolResult
    {
        public bool NeedFileSystemTools { get; set; }
        public bool NeedWeatherTools { get; set; }
        public bool NeedTimeTools { get; set; }
    }
}
   