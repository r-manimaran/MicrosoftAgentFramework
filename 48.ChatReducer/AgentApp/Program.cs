using AgentApp;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

AzureOpenAIClient client = new(new Uri(LLMConfig.Endpoint), new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));
ChatClient chatClient = client.GetChatClient(LLMConfig.DeploymentOrModelId);

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
IChatReducer chatReducer = new MessageCountingChatReducer(targetCount: 4);
IChatReducer chatReducer2 = new SummarizingChatReducer(chatClient.AsIChatClient(), targetCount: 1, threshold: 4);
#pragma warning restore MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


ChatClientAgent agent = client
            .GetChatClient(LLMConfig.DeploymentOrModelId)
            .AsAIAgent(new ChatClientAgentOptions
               {
                   ChatOptions = new()
                   {
                       Instructions = "You are a Friendly AI Bot, answering questions in oneline or less than 20 words",
                   },
                   ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions
                   {
                       ChatReducer = chatReducer
                   })
               });
AgentSession session = await agent.CreateSessionAsync();
while (true)
{
    Console.Write("> ");
    string input = Console.ReadLine() ?? string.Empty;
    AgentResponse response = await agent.RunAsync(input, session);
    Console.WriteLine(response);
    response.Usage.OutputAsInformation();

    InMemoryChatHistoryProvider? provider = agent.GetService<InMemoryChatHistoryProvider>();
    List<ChatMessage> messagesInSession = provider?.GetMessages(session) ?? [];
    Utils.Gray("- Number of messages in session:" + messagesInSession.Count());
    foreach(ChatMessage message in messagesInSession)
    {
        Utils.Gray($"-- {message.Role}:{message.Text}");
    }
    Utils.Separator();
}