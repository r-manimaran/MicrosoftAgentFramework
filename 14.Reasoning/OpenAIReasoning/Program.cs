


using AgentFrameworkToolkit;
using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using OpenAI.Chat;
using OpenAI.Responses;
using OpenAIReasoning;
using System.ClientModel;
using System.Net;
string question = "What is the captial of India and whats the population of india. Answer in 4 words.";

Utils.WriteLineInformation("BaseLine (Reason= Default (Medium)");
await Baseline();
Utils.Separator();
Utils.WriteLineSuccess("Raw: ChatClient (Reason =Minimal)");
await RawChatClient();
Utils.Separator();
Utils.WriteLineSuccess("Raw: Response API (Reason = High)");
await RawResponseApi();
Utils.Separator();
Utils.WriteLineSuccess("Agent Framework Toolkit: ChatClient (Reason = Minimal)");
await AgentFrameworkToolkitChatClient();
Utils.Separator();
Utils.WriteLineSuccess("Agent Framework Toolkit: ResponsesAPI (Reason = High)");
await AgentFrameworkToolkitResponseApi();
Utils.Separator();

return;

async Task Baseline()
{
    AzureOpenAIClient azureOpenAiClient = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new ApiKeyCredential(LLMConfig.ApiKey));
    ChatClientAgent agent = azureOpenAiClient.GetChatClient("gpt-5-mini")
        .AsAIAgent();

    AgentResponse response = await agent.RunAsync(question);
    response.Usage.OutputAsInformation();
    Console.WriteLine(response);
}

async Task RawChatClient()
{
    AzureOpenAIClient azureOpenAiClient = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new ApiKeyCredential(LLMConfig.ApiKey));
    ChatClientAgent agent = azureOpenAiClient.GetChatClient("gpt-5-mini")
        .AsAIAgent(
        options: new ChatClientAgentOptions
        {
            ChatOptions = new Microsoft.Extensions.AI.ChatOptions
            {
                RawRepresentationFactory = _ => new ChatCompletionOptions
                {
                    ReasoningEffortLevel = ChatReasoningEffortLevel.Minimal
                }
            }
        });
    AgentResponse response = await agent.RunAsync(question);
    // Note that the reasoning summary is not possible to get with ChatClient
    Console.WriteLine(response);
    response.Usage.OutputAsInformation();
}

async Task RawResponseApi()
{
    AzureOpenAIClient azureOpenAiClient = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new ApiKeyCredential(LLMConfig.ApiKey));
    ChatClientAgent agent = azureOpenAiClient
        .GetResponsesClient("gpt-5-mini")
        .AsAIAgent(
        options: new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                RawRepresentationFactory = _ => new CreateResponseOptions
                {
                    ReasoningOptions = new ResponseReasoningOptions
                    {
                        ReasoningEffortLevel = ResponseReasoningEffortLevel.High,
                        ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Detailed,
                    }
                }
            }
        });
    AgentResponse response = await agent.RunAsync(question);
    foreach(Microsoft.Extensions.AI.ChatMessage message in response.Messages)
    {
        foreach(AIContent content in message.Contents)
        {
            if(content is TextReasoningContent textReasoningContent)
            {
                Utils.WriteLineWarning("Reasoning Text:");
                Utils.WriteLineSuccess(textReasoningContent.Text);
            }
        }
    }
    Console.WriteLine(response);
    response.Usage.OutputAsInformation();
}

async Task AgentFrameworkToolkitChatClient()
{
    AzureOpenAIAgentFactory agentFactory = new(LLMConfig.Endpoint, LLMConfig.ApiKey);

    AzureOpenAIAgent agent = agentFactory.CreateAgent(new AgentOptions
    {
        Model = OpenAIChatModels.Gpt5Mini,
        ReasoningEffort = OpenAIReasoningEffort.Minimal
    });
    AgentResponse response = await agent.RunAsync(question);
    Console.WriteLine(response);
    response.Usage.OutputAsInformation();
}

async Task AgentFrameworkToolkitResponseApi()
{
    AzureOpenAIAgentFactory agentFactory = new(LLMConfig.Endpoint,LLMConfig.ApiKey);
    AzureOpenAIAgent agent = agentFactory.CreateAgent(new AgentOptions
    {
        Model = OpenAIChatModels.Gpt5Mini,
        ClientType = ClientType.ResponsesApi,
        ReasoningEffort = OpenAIReasoningEffort.High,
        ReasoningSummaryVerbosity = OpenAIReasoningSummaryVerbosity.Detailed
    });
    AgentResponse response = await agent.RunAsync(question);
    Console.WriteLine(response);
    TextReasoningContent? reasoningContent = response.GetTextReasoningContent();
    if(reasoningContent != null)
    {
        Utils.WriteLineWarning("Reasoning Text");
        Utils.WriteLineInformation(reasoningContent.Text);
    }
    response.Usage.OutputAsInformation();
}