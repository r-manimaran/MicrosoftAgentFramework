using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Core.Extensions;
using Azure.Identity;
using FileSearchTool;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Shared;

PersistentAgentsClient client = new PersistentAgentsClient(LLMConfig.AzureAiFoundaryAgentEndpoint, new AzureCliCredential());
Response<PersistentAgent>? aiFoundryAgent = null; 

ChatClientAgentThread? chatClientAgentThread = null;
string? vectorStoreId = null;

try
{
    string fileName = "MySecretData.pdf";
    Response<PersistentAgentFileInfo> fileInfo = await client.Files.UploadFileAsync(Path.Combine("Data", fileName), PersistentAgentFilePurpose.Agents);
    Response<PersistentAgentsVectorStore> vectorStore = await client.VectorStores.CreateVectorStoreAsync(name: "myVectorStore");
    vectorStoreId = vectorStore.Value.Id;
    await client.VectorStores.CreateVectorStoreFileAsync(vectorStoreId, fileInfo.Value.Id);

    aiFoundryAgent = await client.Administration.CreateAgentAsync(
        LLMConfig.DeploymentOrModelId,
        "FileAgent",
        "",
        "You are a File-expert. ALWAYS use tools to answer all questions. Do not use your world-knowledge",
        toolResources: new ToolResources
        {
            FileSearch = new FileSearchToolResource
            {
                VectorStoreIds = { vectorStore.Value.Id },

            }
        },
        tools: new List<ToolDefinition> 
        {
            new FileSearchToolDefinition
            {
                FileSearch = new FileSearchToolDefinitionDetails
                {
                    MaxNumResults =10
                }
            }
        });

    AIAgent agent = (await client.GetAIAgentAsync(aiFoundryAgent.Value.Id));
   
    AgentThread thread = agent.GetNewThread();
    AgentRunResponse response = await agent.RunAsync("What is word of the day?", thread);
    Utils.WriteLineInformation($"Response: {response}");

    foreach(ChatMessage message in response.Messages)
    {
        foreach(AIContent content in message.Contents)
        {
           foreach(AIAnnotation annotation in content.Annotations ?? [])
            {
                if(annotation is CitationAnnotation citationAnnotation)
                {
                    string? fileId = citationAnnotation.FileId;
                    Response<PersistentAgentFileInfo> fileReferenced = await client.Files.GetFileAsync(fileId);
                    Utils.WriteLineInformation($"Citation from file: {fileReferenced.Value.Filename}");

                }
            }
        }
    }
}

catch(Exception ex)
{
    Utils.WriteLineError($"Error: {ex.Message}");
}
finally
{
    if (vectorStoreId !=null)
    {
        await client.VectorStores.DeleteVectorStoreAsync(vectorStoreId);
    }
}