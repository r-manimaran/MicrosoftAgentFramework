using Azure.AI.OpenAI;
using Azure.AI.Projects;
using CustomChatHistoryReducer;
using CustomChatHistoryReducer.Reducers;
using CustomChatHistoryReducer.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
Console.Clear();
Utils.Gray("---Custom Chat Reducer---");
AzureOpenAIClient client = new(new Uri(LLMConfig.Endpoint), new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));
ChatClient chatClient = client.GetChatClient(LLMConfig.DeploymentOrModelId);
#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
// Reducer 1
IChatReducer alwaysRemoveToolCallsReducer = new AlwaysRemoveToolCallsReducer();

// Reducer 2
IChatReducer messageWithWordReducer = new MessageWithWordReducer("Sunny");

// Reducer 3
ChatClientAgent pirateSummaryReducerAgent = client.GetChatClient("gpt-4o-mini")
    .AsAIAgent(instructions: "Given the input messages make a summary of them in the voice of a pirate!");
AIDrivenPirateSummaryReducer aIDrivenPirateSummaryReducer = new AIDrivenPirateSummaryReducer(pirateSummaryReducerAgent, 4);

// Reducer 4
ChatClientAgent cityReducerAgent = client.GetChatClient("gpt-4o-mini")
    .AsAIAgent(instructions: "Given the input numbered messages, return the numbers of the messages that contain a city");
AIDrivenCityReducer aiDrivenCityReducer = new AIDrivenCityReducer(cityReducerAgent);

ChatClientAgent agent = client
            .GetChatClient(LLMConfig.DeploymentOrModelId)
            .AsAIAgent(new ChatClientAgentOptions
            {
                ChatOptions = new()
                {
                    Instructions = "You are a Friendly AI Bot, answering questions in oneline or less than 20 words",
                    Tools = [AIFunctionFactory.Create(CustomTools.GetWeather,"get_weather")]
                },
                ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions
                {
                    // ChatReducer = alwaysRemoveToolCallsReducer
                    //ChatReducer = aIDrivenPirateSummaryReducer
                    ChatReducer = aiDrivenCityReducer
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
    foreach (ChatMessage message in messagesInSession)
    {
        if (!string.IsNullOrWhiteSpace(message.Text))
        {
            Utils.Gray($"-- {message.Role}:{message.Text}");
        }
        else
        {
            foreach(AIContent content in message.Contents)
            {
                switch (content)
                {
                    case FunctionCallContent functionCallContent:
                        Utils.Gray($"-- [{message.Role}] Tool Call {functionCallContent.Name} [Args :{string.Join(",", functionCallContent.Arguments)}]");
                        break;
                    case FunctionResultContent functionResultContent:
                        Utils.Gray($"--[{message.Role}] Tool Result:{functionResultContent.Result}");
                        break;                          
                }
            }
        }
    }
    Utils.Separator();
}

#pragma warning restore MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
