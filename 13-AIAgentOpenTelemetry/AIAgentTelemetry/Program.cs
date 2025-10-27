using OpenTelemetry;
using SharedLib;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Options;
using Azure.AI.OpenAI;
using System.ClientModel;
using Microsoft.Agents.AI;
using OpenAI;
using Azure.Monitor.OpenTelemetry.Exporter;


string sourceName = "AiSource";
var traceProviderBuilder = Sdk.CreateTracerProviderBuilder()
                              .AddSource(sourceName)
                              .AddConsoleExporter();

if(!string.IsNullOrWhiteSpace(LLMConfig.ApplicationInsightsConnectionString))
{
    traceProviderBuilder.AddAzureMonitorTraceExporter(Options=> Options.ConnectionString = LLMConfig.ApplicationInsightsConnectionString);
}
using TracerProvider tracerProvider = traceProviderBuilder.Build();

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new ApiKeyCredential(LLMConfig.ApiKey));
AIAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .CreateAIAgent(name:"MyObservedAgent",
    instructions: "You are a Friendly AI Bot, answering questions")
    .AsBuilder()
    .UseOpenTelemetry(sourceName,options=>
    {
        options.EnableSensitiveData = true;
    })
    .Build();
AgentThread thread = agent.GetNewThread();

AgentRunResponse response1 = await agent.RunAsync("Hello, My name is Mani", thread);
Utils.WriteLineInformation(response1.Text);
Utils.Separator();

AgentRunResponse response2 = await agent.RunAsync("What is the capital of France?", thread);
Utils.WriteLineInformation(response2.Text);
Utils.Separator();

AgentRunResponse response3 = await agent.RunAsync("What was the previous question?", thread);
Utils.WriteLineInformation(response3.Text);
Utils.Separator();



