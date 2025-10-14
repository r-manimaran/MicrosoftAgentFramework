
using Azure.AI.OpenAI;
using ConsoleApp;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using SharedLib;
using System.ClientModel;

AzureOpenAIClient client = new AzureOpenAIClient(
    new Uri(LLMConfig.Endpoint), new ApiKeyCredential(LLMConfig.ApiKey));

AIAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
                      .CreateAIAgent(instructions: "You are a friendly AI Bot, answering questions in a friendly manner.");

// create a Thread
AgentThread thread = agent.GetNewThread();

const bool optionToResume = true;
if (optionToResume)
{
    // Resume from previous conversation
    thread = await AgentThreadPersistence.ResumeChatIfRequestedAsync(agent);
}
while(true)
{
    Console.Write("> ");
    string? userInput = Console.ReadLine();
    if (string.IsNullOrEmpty(userInput))
    {
       Utils.WriteLineError("Empty input, exiting.");
       break;
    }
    ChatMessage message = new ChatMessage(ChatRole.User, userInput);

    await foreach(AgentRunResponseUpdate update in agent.RunStreamingAsync(message, thread))
    {
        Console.Write(update);
    }

    Console.WriteLine();
    Console.WriteLine(string.Empty.PadLeft(50, '*'));
    Console.WriteLine();

    if(optionToResume)
    {
        await AgentThreadPersistence.StoreThreadAsync(thread);
    }
}

