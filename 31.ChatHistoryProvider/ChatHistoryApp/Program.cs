using Azure.AI.OpenAI;
using ChatHistoryApp;
using Microsoft.Agents.AI;
using OpenAI.Chat;
using System.Text.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

Console.Clear();
AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
                    new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));

ChatClientAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatHistoryProvider = new MyMessageStore()
    });
AgentSession session = await agent.CreateSessionAsync();

AgentResponse response = await agent.RunAsync("Who is Barack obama", session);
Console.WriteLine(response);

JsonElement sessionElement = await agent.SerializeSessionAsync(session);
string toStoreForTheUser = JsonSerializer.Serialize(sessionElement);

Utils.Separator();

Utils.WriteLineSuccess("Some time passes, and we restore the session...");

JsonElement restoredSessionElement = JsonSerializer.Deserialize<JsonElement>(toStoreForTheUser);

AgentSession restoredSession = await agent.DeserializeSessionAsync(restoredSessionElement);

AgentResponse simeTimeLaterResponse = await agent.RunAsync("How Tall is he?", restoredSession);
Console.WriteLine(simeTimeLaterResponse);

Console.ReadLine();

class MyMessageStore() : ChatHistoryProvider
{
    public string? SessionId { get; set; }

    public string SessionPath => Path.Combine(Path.GetTempPath(), $"{SessionId}.json");

    private readonly List<ChatMessage> _messages = [];

    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        if(context.Session!.StateBag.TryGetValue("SessionId", out string? sessionId))
        {
            SessionId = sessionId;
        }
        else
        {
            SessionId = Guid.NewGuid().ToString();
            context.Session.StateBag.SetValue("SessionId", SessionId);
        }

        if (!File.Exists(SessionPath))
        {
            return [];
        }
        string json = await File.ReadAllTextAsync(SessionPath, cancellationToken);
        return JsonSerializer.Deserialize<List<ChatMessage>>(json)!;
    }

    protected override async ValueTask StoreChatHistoryAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        // Add both request and response messages to the store
        // optionally messages produced by the AIContextProvider can also be presisted (not shown).
        _messages.AddRange(context.RequestMessages.Concat(context.ResponseMessages ?? []));

        await File.WriteAllTextAsync(SessionPath, JsonSerializer.Serialize(_messages), cancellationToken);
    }
}