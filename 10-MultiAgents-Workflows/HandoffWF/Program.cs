
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using SharedLib;
using System.Text.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
                        new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));

ChatClient chatClient = client.GetChatClient(LLMConfig.DeploymentOrModelId);

AIAgent intentAgent = chatClient.CreateAIAgent(name: "IntentAgent", instructions: "Determine what type of question was asked. Don't answer directly by yourself.");
AIAgent movieNerd = chatClient.CreateAIAgent(name: "MovieNerd", instructions: "You are a movie nerd. You can answer questions about movies.");
AIAgent musicNerd = chatClient.CreateAIAgent(name: "MusicNerd", instructions: "You are a music nerd. You can answer questions about music.");

while (true)
{
    List<ChatMessage> messages = [];
    Workflow workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(intentAgent)
        .WithHandoffs(intentAgent, [movieNerd,musicNerd])
        .WithHandoffs([movieNerd,musicNerd],intentAgent)
        .Build();
    Console.Write("> ");
    messages.Add(new(ChatRole.User, Console.ReadLine()!));
    messages.AddRange(await RunWorkflowAsync(workflow, messages));
}

static async Task<List<ChatMessage>> RunWorkflowAsync(Workflow workflow, List<ChatMessage> messages)
{
    string? lastExecutorId = null;
    StreamingRun run = await InProcessExecution.StreamAsync(workflow, messages);
    await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
    await foreach(WorkflowEvent @event in run.WatchStreamAsync())
    {
        switch (@event)
        {
            case AgentRunUpdateEvent e:
                {
                    if (e.ExecutorId != lastExecutorId)
                    {
                        lastExecutorId = e.ExecutorId;
                        Console.WriteLine();
                        Utils.WriteLineSuccess(e.Update.AuthorName ?? e.ExecutorId);
                    }
                    Console.Write(e.Update.Text);
                    if(e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                    {
                        Console.WriteLine();
                        Utils.WriteLineInformation($"Call '{call.Name}' with arguments: [ {JsonSerializer.Serialize(call.Arguments)}]");
                    }
                    break;
                }
            case WorkflowOutputEvent output:
               Utils.Separator();
               return output.As<List<ChatMessage>>()!;
                
        }
    }
    return [];
}