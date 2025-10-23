using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using SharedLib;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
                        new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));

ChatClient chatClient = client.GetChatClient(LLMConfig.DeploymentOrModelId);

string classicFanAgentInstructions = "You are a movie lover and recommends always timeless classic movies in tamil.";
string romanceFanAgentInstructions = "You are a movie lover and recommends romantic movies in tamil.";
string actionFanAgentInstructions = "You are a movie lover and recommends action movies in tamil.";

AIAgent classicAgent = chatClient.CreateAIAgent(name:"classicAgent", instructions: classicFanAgentInstructions);
AIAgent romanceAgent = chatClient.CreateAIAgent(name:"romanceAgent", instructions: romanceFanAgentInstructions);
AIAgent actionAgent = chatClient.CreateAIAgent(name:"actionAgent", instructions: actionFanAgentInstructions);

string input = "Suggest a movie to watch for this weekend.";

// Create the workflow
Workflow workflow = AgentWorkflowBuilder.BuildConcurrent([classicAgent, romanceAgent, actionAgent]);

var messages = new List<ChatMessage> { new(ChatRole.User, input) };

StreamingRun run = await InProcessExecution.StreamAsync(workflow, messages);

await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

List<ChatMessage> result = new();

await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
{
    if (evt is WorkflowOutputEvent completed)
    {
        result = (List<ChatMessage>)completed.Data!;
        break;
    }
}

foreach (ChatMessage message in result.Where(x => x.Role != ChatRole.User))
{
    Console.WriteLine(message.AuthorName ?? "Unknown Author");
    Console.WriteLine($"{message.Text}\n");
    Console.WriteLine("--------------------------------------------------");
}