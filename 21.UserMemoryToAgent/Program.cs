using _21.UserMemoryToAgent;
using Azure.AI.OpenAI;
using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

string userId = "user123";

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new ApiKeyCredential(LLMConfig.ApiKey));
ChatClientAgent memoryExtractorAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .CreateAIAgent(
    instructions: "Look at the user's message and extract any memory that we do not already know");

ChatClientAgent agentWithCustomerMemory = client.GetChatClient(LLMConfig.DeploymentOrModelId).AsIChatClient()
                        .CreateAIAgent(new ChatClientAgentOptions                       
                             {
                                AIContextProviderFactory = _ => new CustomContextProvider(memoryExtractorAgent, userId)
                             });

AIAgent agentToUse = agentWithCustomerMemory;
AgentThread agentThread = agentToUse.GetNewThread();
while (true) {     
    Console.Write("You: ");
    string userInput = Console.ReadLine() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(userInput))
    {
        break;
    }

    ChatMessage message = new ChatMessage(ChatRole.User, userInput);
    AgentRunResponse response = await agentToUse.RunAsync(message, agentThread);
    Console.WriteLine($"Agent: {response}");
}


class CustomContextProvider : AIContextProvider
{
    private readonly ChatClientAgent _memoryExtractorAgent;
    private readonly List<string> _userFacts = [];
    private readonly string _userMemoryFilePath;

    public CustomContextProvider(ChatClientAgent memoryExtractorAgent, string userId)
    {
        _memoryExtractorAgent = memoryExtractorAgent;
        _userMemoryFilePath = Path.Combine(Path.GetTempPath(), $"{userId}_memory.txt");
        if(File.Exists(_userMemoryFilePath))
        {
            var lines = File.ReadAllLines(_userMemoryFilePath);
            _userFacts.AddRange(lines);
        }
    }
    /// <summary>
    /// This method is called before the agent invokes the LLM.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override ValueTask<AIContext> InvokingAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        return new ValueTask<AIContext>(new AIContext
        {
            Instructions = string.Join(" | ", _userFacts)
        });
    }

    /// <summary>
    /// This method is called after the agent has invoked the LLM.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async ValueTask InvokedAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        ChatMessage lastMessageFromUser = context.RequestMessages.Last();
        List<ChatMessage> inputToMemoryExtractor = [
            new(ChatRole.Assistant, $"We know the following about the user already and should not extract that again:{string.Join(" | ", _userFacts)}"),
            lastMessageFromUser
            ];

        ChatClientAgentRunResponse<MemoryUpdate> response = await _memoryExtractorAgent.RunAsync<MemoryUpdate>(inputToMemoryExtractor, cancellationToken: cancellationToken);
        foreach(string memoryToRemove in response.Result.MemoriesToRemove)
        {
            _userFacts.Remove(memoryToRemove);
        }
        _userFacts.AddRange(response.Result.MemoriesToAdd);
        await File.WriteAllLinesAsync(_userMemoryFilePath, _userFacts.Distinct(), cancellationToken);
    }

    [UsedImplicitly]
    private record MemoryUpdate(List<string> MemoriesToAdd, List<string> MemoriesToRemove);
}