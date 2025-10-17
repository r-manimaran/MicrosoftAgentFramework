
using Azure.AI.OpenAI;
using Azure.Core;
using ConsoleApp;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System.ClientModel;

AzureOpenAIClient client = new AzureOpenAIClient( new Uri(LLMConfig.Endpoint), new ApiKeyCredential(LLMConfig.ApiKey));

AIAgent noSettingAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId).CreateAIAgent();

AIAgent customSettingAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId).CreateAIAgent(
                                instructions: "You are a helpful assistant that translates English to French.",
                                tools: [/* Add tools here if needed */]);

#region Lets set all the options-parameters
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(new MySpecialService());
ServiceProvider ServiceProvider = builder.Services.BuildServiceProvider();

// OpenTelemetry
string sourceName = Guid.NewGuid().ToString("N");
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter()
    .Build();

AIAgent agentWithAllSettings = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .CreateAIAgent(

    // Optional system instructions to guide the agent's behavior.
    instructions: "You are a helpful assistant that translates English to French.",

    /******************************************************************/

    // optional name for the agent for identification purposes.
    name: "TranslatorAgent",

    /******************************************************************/

    // optional description for the agent to provide context about its purpose.
    description: "An agent that translates English text to French.",

    /******************************************************************/

    // Optional list of tools the agent can use to perform tasks.
    tools: [/* Add tools here if needed */],

    
    /******************************************************************/
    //provides a way to customize the creation of the underlying IChatClient used by the agent.
   clientFactory: chatClient =>
    {
        return new ConfigureOptionsChatClient(chatClient, options =>
        {
            options.MaxOutputTokens = 1024;
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            options.RawRepresentationFactory = _ => new ChatCompletionOptions
            {
                ReasoningEffortLevel = ChatReasoningEffortLevel.Low,               

            };
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        });
    }, // Unrecognized request argument supplied: reasoning_effort'
    /******************************************************************/
    // optional logger factory for enabling logging within the agent.
    loggerFactory: LoggerFactory.Create(loggingBuilder => { loggingBuilder.AddConsole(); }),

    /******************************************************************/
    // optional service provider for dependency injection, allowing the agent to access registered services.
    services: ServiceProvider

    )
    .AsBuilder()
    .UseOpenTelemetry(sourceName) // Integrate OpenTelemetry for tracing and monitoring.
    .Build();

AgentRunResponse response = await agentWithAllSettings.RunAsync("Translate the following English text to French: 'Hello, how are you?'");
Console.WriteLine(response);
#endregion

# region More options with chatClientAgentOptions
AIAgent advancedAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId).CreateAIAgent(new ChatClientAgentOptions
{
    ChatOptions = new ChatOptions
    {
        MaxOutputTokens = 1024,
    },
    AIContextProviderFactory = null, //option to intercept before and after each LLM call
    ChatMessageStoreFactory = null, //option to customize the chat message store
    Instructions = "You are a helpful assistant that translates English to French.",
    Description = "An agent that translates English text to French.",
    Id = "TranslatorAgent",
    UseProvidedChatClientAsIs = false
},
clientFactory: chatClient =>
{
        return new ConfigureOptionsChatClient(chatClient, options =>
     {
         options.MaxOutputTokens = 1024;
     });
    },
loggerFactory: LoggerFactory.Create(loggingBuilder => { loggingBuilder.AddConsole(); }),
services: ServiceProvider
).AsBuilder()
    .UseOpenTelemetry(sourceName) // Integrate OpenTelemetry for tracing and monitoring.
    .Build();




public class MySpecialService
{
    public string GetInfo() => "This is some special service information.";
}