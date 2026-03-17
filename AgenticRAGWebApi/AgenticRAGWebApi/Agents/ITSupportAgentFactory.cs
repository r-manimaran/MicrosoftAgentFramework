using AgenticRAGWebApi.Tools;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgenticRAGWebApi.Agents;

public class ITSupportAgentFactory(AgentTools tools, IConfiguration configuration)
{
    public AIAgent Create()
    {
        // 1. Build the azure OpenAI chat client (IChatClient)
        var chatClient = new AzureOpenAIClient(new Uri(configuration["AzureOpenAI:Endpoint"]!),
            new DefaultAzureCredential())
            .GetChatClient(configuration["AzureOpenAI:Deployment"]!);

        // 2. Register tools using AIFunctionFactory.Create()
        // Each tool is a plain method 
        AIFunction[] agentTools =
            [
                AIFunctionFactory.Create(
                    tools.SearchRunbooksAsync,
                    name: "search_runbooks",
                    description:"Search IT runbooks with hybrid semantic + keyword search"),

                AIFunctionFactory.Create(
                    tools.GetT)


            ];
    }
}
