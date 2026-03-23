using AgentFrameworkToolkit.Tools.Common;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.Text;
using ToolCallingInjection;
using ToolCallingInjection.Extensions;

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
    ChatClientAgent toolnjectionAgent = client.GetChatClient("gpt-4o-mini").AsAIAgent(
        instructions: "Your job is to tell if any given message is a request to use specific tools");

    AIAgent mainAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId).AsAIAgent(new ChatClientAgentOptions
    {
        AIContextProviders = (_, _) => ValueTask.FromResult<AIContextProvider>(new OnTheFlyToolInjectionContext(toolinjectionAgent))
    }).AsBuilder().Use(FunctionCallMiddleware).Build();

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

class OnTheFlyToolInjectionContext(ChatClientAgent toolInjectionAgent): AIContextProvider
{
    public override 
}