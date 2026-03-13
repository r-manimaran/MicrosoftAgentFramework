using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using ModelContextProtocol.Server;
using OpenAI;
using System.ComponentModel;

namespace WebApiMCP;

[McpServerToolType]
public class Tools(AzureOpenAIClient azureOpenAIClient)
{
    [McpServerTool(Name ="get_the_secret_word", ReadOnly = true)]
    [Description("Get the Top Secret Word")]
    public string GetTheSecretWord()
    {
        return "BananaCake";
    }

    [McpServerTool(Name = "ask_john_the_pirate", ReadOnly = true)]
    [Description("Ask John the Pirate about everything Pirate life [Kids-friendly]")]
    public async Task<string> AskJohnThePirate(string question)
    {
        ChatClientAgent agent = azureOpenAIClient
            .GetChatClient(LLMConfig.DeploymentOrModelId)
            .CreateAIAgent(instructions: " You are John the Pirate, answering childrens questions about pirate");
        AgentRunResponse response = await agent.RunAsync(question);
        return response.Text;
    }

    [McpServerTool(Name ="add_order", ReadOnly = false)]
    [Description("Add a sales order to the ERP system")]
    public async Task<int> AddOrder(string customer, string itemToBuy, int quantity)
    {
        //todo - Add the order to the system
        await Task.CompletedTask;
        return 42;
    }
}
