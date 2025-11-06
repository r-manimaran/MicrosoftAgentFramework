using AgentApp;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new AzureKeyCredential(LLMConfig.ApiKey), options: new AzureOpenAIClientOptions()
{
    NetworkTimeout = TimeSpan.FromMinutes(10)
});


string question = "can you please list top 10 tamil movies in IMDB?";

// Without structured output
AIAgent agent1 = client.GetChatClient(LLMConfig.DeploymentOrModelId).CreateAIAgent(instructions: "You are an expert in IMDB movies lists");
AgentRunResponse agentRunResponse = await agent1.RunAsync(question);
Console.WriteLine("Response without structured output:");
Console.WriteLine(agentRunResponse);
agentRunResponse?.Usage.OutputAsInformation();

Utils.Separator();
Console.ReadLine();

// Lets control the Reasoning and output cost
ChatClientAgent agentControllingReasoningEffort = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .CreateAIAgent(
    options: new ChatClientAgentOptions
    {
        ChatOptions = new Microsoft.Extensions.AI.ChatOptions
        {
            RawRepresentationFactory = _ => new ChatCompletionOptions
            {
#pragma warning disable OPENAI001
                ReasoningEffortLevel = "minimal", // possible values: minimal, low, medium (default), high
#pragma warning restore OPENAI001
            },
        }
    });

AgentRunResponse agentRunResponse2 = await agentControllingReasoningEffort.RunAsync(question);
Console.WriteLine("Response with controlled reasoning effort (minimal):");
Console.WriteLine(agentRunResponse2);
agentRunResponse2?.Usage.OutputAsInformation();
Utils.Separator();
Console.ReadLine();

// Simpler option with extension method
ChatClientAgent agentControllingReasoningEffort2 = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .CreateAIAgentForAzureOpenAI(reasoningeffort: "minimal");
AgentRunResponse agentRunResponse3 = await agentControllingReasoningEffort2.RunAsync(question);
Console.WriteLine("Response with controlled reasoning effort (minimal) using extension method:");
Console.WriteLine(agentRunResponse3);
agentRunResponse3?.Usage.OutputAsInformation();
Utils.Separator();
Console.ReadLine();
