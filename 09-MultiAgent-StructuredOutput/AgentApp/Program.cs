using AgentApp;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));

ChatClient chatClientMini = client.GetChatClient(LLMConfig.DeploymentOrModelId);
ChatClient chatClient = client.GetChatClient(LLMConfig.DeploymentOrModelId);

Console.Write("> ");
string userInput = Console.ReadLine() ?? string.Empty;

// Determine the intent of the user input


AIAgent intentAgent = chatClientMini.CreateAIAgent("IntentAgent",
                "You are an AI agent that determines the intent of the user input. " +
                "The intent can be: " +
                "1. Movies: The user asks about Movies " +
                "2. Music: The user wants to search for Music related items " +
                "3. Others: The user input is not clear or does not match any of the above intents. " +
                "You will respond with the intent and nothing else.");

JsonSerializerOptions jsonSerializationOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
};

AgentRunResponse initialResponse = await intentAgent.RunAsync(userInput, options: new ChatClientAgentRunOptions()
{
    ChatOptions = new ChatOptions
    {
        ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema<IntentResponse>(jsonSerializationOptions)
    }
});

IntentResponse intentResult = initialResponse.Deserialize<IntentResponse>(jsonSerializationOptions)!;
Console.WriteLine($"Detected Intent: {intentResult.Intent}");

switch(intentResult.Intent)
{
    case Intent.Movies:
        Console.WriteLine("You have selected the Movies intent. Here are some movie recommendations...");
        // Add logic to handle Movies intent
        AIAgent movieAgent = chatClient.CreateAIAgent("MovieAgent",
            "You are an AI agent that provides movie recommendations based on user preferences. " +
            "provides the answer not more than 200 characters. " +
            "You will respond with the movie recommendations and nothing else.");
        AgentRunResponse responseMovie = await movieAgent.RunAsync(userInput);
        Console.WriteLine(responseMovie);

        break;
    case Intent.Music:
        Console.WriteLine("You have selected the Music intent. Here are some music recommendations...");
        // Add logic to handle Music intent
        AIAgent musicAgent = chatClient.CreateAIAgent("MusicAgent",
            "You are an AI agent that provides music recommendations based on user preferences. " +
            "provides the answer not more than 200 characters. " +
            "You will respond with the music recommendations and nothing else.");
        AgentRunResponse responseMusic = await musicAgent.RunAsync(userInput);
        Console.WriteLine(responseMusic);
        break;
    case Intent.Others:
        Console.WriteLine("The intent is unclear. Please provide more details.");
        // Add logic to handle Others intent
        AIAgent othersAgent = chatClient.CreateAIAgent("OthersAgent",
            "You are an AI agent that provides recommendations based on user preferences. " +
            "provides the answer not more than 200 characters. " +
            "You will respond with the recommendations and nothing else.");
        AgentRunResponse responseOthers = await othersAgent.RunAsync(userInput);
        Console.WriteLine(responseOthers);
        break;
    default:
        throw new ArgumentOutOfRangeException();
}
Console.ReadLine();
public class IntentResponse
{
    [Description("The intent of the user input")]
    public required Intent Intent { get; set; }
}
public enum Intent
{
    Movies,
    Music,
    Others
}