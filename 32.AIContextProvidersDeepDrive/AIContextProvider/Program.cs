using AgentFrameworkToolkit;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;
using System.ClientModel.Primitives;

Utils.Init("AIContextProviders");
(Uri endpoint, ApiKeyCredential apiKey) = SecretsManager.GetAzureOpenAICredentials();

AzureOpenAIClient client = new(endpoint, apiKey, new AzureOpenAIClientOptions
{
    Transport = new HttpClientPipelineTransport(new HttpClient(new RawCallDetailsHttpHandler(r=>r.RequestUrl="")))
});

AIAgent agent = client.GetChatClient("gpt-4o-mini")
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new Microsoft.Extensions.AI.ChatOptions
        {
            Instructions = "Prefix all messages with 👌👌👌"
        },
        AIContextProviders = [

            ]
    }).AsBuilder()
    .Build();

AgentSession session = await agent.CreateSessionAsync();
while  (true)
{
    Console.Write(">");
    string input = Console.ReadLine() ?? "";
    AgentResponse response = await agent.RunAsync(input,session);
    Utils.Separator();
    Utils.Red("Final Response:");
    Console.WriteLine(response);
    Utils.Separator();
}