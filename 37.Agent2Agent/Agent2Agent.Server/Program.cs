// -- Server
using A2A;
using A2A.AspNetCore;
using Agent2Agent.Server.Tools;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Shared;
using System.Reflection;
using System.Text;
using OpenAI.Chat;


Console.Clear();
var secrets = SecretsManager.GetAzureOpenAICredentials();

AzureOpenAIClient client = new(secrets.endpoint, secrets.apiKey);

FileSystemTools target = new FileSystemTools();
MethodInfo[] methods = typeof(FileSystemTools).GetMethods(BindingFlags.Public | BindingFlags.Instance);
List<AITool> listOfTools = methods.Select(x=> AIFunctionFactory.Create(x,target)).Cast<AITool>().ToList();

AIAgent agent = client.GetChatClient("gpt-4o-mini").AsAIAgent(
                    name:"FileAgent",
                    instructions:"You are a File Expert. When working with files you need to provide the full path; not just the filename",
                    tools: listOfTools)
                .AsBuilder()
                .Use(FunctionCallMiddleware)
                .Build();

// -- A2A part begin here
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();

AgentCard agentCard = new AgentCard() // Aka the Agents Business Card
{
    Name = "FilesAgent",
    Description = "Handles request relating to files",
    Version = "1.0.0",
    DefaultInputModes = ["text"],
    DefaultOutputModes = ["text"],
    Capabilities = new AgentCapabilities()
    {
        Streaming = false,
        PushNotifications = false,
    },
    Skills = [
        new AgentSkill(){
            Id = "my-file_agent",
            Name="File Expert",
            Description = "Handles requests relating to files on hard disk",
            Tags = ["files","folders"],
            Examples = ["What files are there in Folder 'Demo1'"],
        }
        ],
    Url = "http://localhost:5000"
};
app.MapA2A(agent, path: "/",
           agentCard: agentCard,
           taskManager => app.MapWellKnownAgentCard(taskManager, "/"));
await app.RunAsync();
return;
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
    Utils.WriteLine(functionCallDetails.ToString(), ConsoleColor.DarkGray);
    return await next(context, cancellationToken);
}