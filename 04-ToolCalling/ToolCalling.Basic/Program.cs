using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using ToolCalling.Basic;

AzureOpenAIClient client  = new AzureOpenAIClient(new Uri(AIConfig.Endpoint),new ApiKeyCredential(AIConfig.ApiKey));

AIAgent agent = client.GetChatClient(AIConfig.DeploymentOrModelId)
    .CreateAIAgent(instructions: "You are a time expert",
    tools: [
        AIFunctionFactory.Create(Tools.CurrentDateAndTime,"current_date_and_time"),
        AIFunctionFactory.Create(Tools.CurrentTimezone,"current_timezone")
        ]);

AgentThread thread = agent.GetNewThread();

while(true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    ChatMessage userMessage = new ChatMessage(ChatRole.User, input ?? string.Empty);
    AgentRunResponse response = await agent.RunAsync(userMessage, thread);
    Console.WriteLine(response);

    Console.WriteLine("************************************");
}
