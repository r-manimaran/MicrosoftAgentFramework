using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using FileSearchTool;
using Microsoft.Agents.AI;
using OpenAI;
using Shared;

PersistentAgentsClient client = new PersistentAgentsClient(LLMConfig.AzureAiFoundaryAgentEndpoint, new AzureCliCredential());
BingGroundingSearchConfiguration bingToolConfiguration = new BingGroundingSearchConfiguration(LLMConfig.BinApiKey);
BingGroundingSearchToolParameters bingToolParameters = new BingGroundingSearchToolParameters([bingToolConfiguration]);

Response<PersistentAgent>? aiFoundryAgent = null;
ChatClientAgentThread? chatClientAgentThread = null;
try
{
    aiFoundryAgent = await client.Administration.CreateAgentAsync(
        LLMConfig.DeploymentOrModelId,
        "CurrentNewsAgent",
        "",
        "You are a news expert. ALWAYS use tools to answer all questions. Do not use your world-knowledge",
        tools: new List<ToolDefinition>
        {
            new BingGroundingToolDefinition(bingToolParameters)
        });
    AIAgent agent = (await client.GetAIAgentAsync(aiFoundryAgent.Value.Id));

    AgentThread thread = agent.GetNewThread();
    List<AgentRunResponseUpdate> updates = [];
    await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync("what is today's news in Cricket (List today's date at the top)", thread))
    {
        updates.Add(update);
        Console.Write(update);
    }

    AgentRunResponse fullResponse = updates.ToAgentRunResponse();
    fullResponse.Usage?.OutputAsInformation();


}
catch (Exception ex)
{
    Shared.Utils.WriteLineError($"Error: {ex.Message}");
}
finally
{
    if (chatClientAgentThread != null)
    {
        await client.Threads.DeleteThreadAsync(chatClientAgentThread.ConversationId);
    }

    if (aiFoundryAgent != null)
    {
        await client.Administration.DeleteAgentAsync(aiFoundryAgent.Value.Id);
    }
}


