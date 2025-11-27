using Azure.AI.OpenAI;
using ConsoleApp;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenAI;
using System.Text.Json;

string host = "localhost";
int port = 10260;

AzureOpenAIClient client = new(new Uri(LLMConfig.Endpoint), new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));
AIAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId).CreateAIAgent();
//AgentRunResponse response = await agent.RunAsync("What's the capital of India.?");
//Console.WriteLine(response);
//Console.ReadLine();

AgentThread thread = agent.GetNewThread();

const bool optionToResume = true;
//if (optionToResume)
//{
//    // Resume from previous conversation
//    thread = await AgentThreadPersistence.ResumeChatIfRequestedAsync(agent);
//}

var credentialDb = MongoCredential.CreateCredential("admin", LLMConfig.DocumentDBUserName, LLMConfig.DocumentDBPassword);
MongoClientSettings settings = new()
{
    Credential = credentialDb,
    Server = new MongoServerAddress(host, port),
    UseTls = true,
    AllowInsecureTls = true,
};
try
{

    using MongoClient mongoclient = new(settings);
    var db = mongoclient.GetDatabase("ThreadData");
    var collection = db.GetCollection<BsonDocument>("ThreadsCollection");
    while (true)
    {
        Console.Write("User > ");
        string? userInput = Console.ReadLine();
        if (string.IsNullOrEmpty(userInput))
        {
            Utils.WriteLineError("Empty input, exiting.");
            break;
        }
        ChatMessage message = new ChatMessage(ChatRole.User, userInput);

        await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync(message, thread))
        {
            Console.Write(update);
        }
        JsonElement serializeThread = thread.Serialize();
        var bsonDoc = BsonDocument.Parse(serializeThread.GetRawText());
        await collection.InsertOneAsync(bsonDoc);
        Console.Write($"{serializeThread.GetRawText()}");

        Console.WriteLine();
        Console.WriteLine(string.Empty.PadLeft(50, '*'));
        Console.WriteLine();
    }
}
catch(Exception ex)
{
    Console.WriteLine(ex.ToString());
}