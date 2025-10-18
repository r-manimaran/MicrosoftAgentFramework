using AgentApp;
using AgentApp.Models;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using SharedLib;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),new AzureKeyCredential(LLMConfig.ApiKey));

string question = "can you please list top 10 tamil movies in IMDB?";

// Without structured output
AIAgent agent1 = client.GetChatClient(LLMConfig.DeploymentOrModelId).CreateAIAgent(instructions: "You are an expert in IMDB movies lists");
AgentRunResponse agentRunResponse = await agent1.RunAsync(question);
Console.WriteLine("Response without structured output:");
Console.WriteLine(agentRunResponse);

Utils.Separator();

// With structured output
// Here use ChatClientAgent instead of AIAgent. Because AIAgent RunAsync does not have <T> option to run
ChatClientAgent agent2 = client.GetChatClient(LLMConfig.DeploymentOrModelId).CreateAIAgent("You are an expert in IMDB movies lists");
AgentRunResponse<MovieResult> agentRunResponse2 = await agent2.RunAsync<MovieResult>(question);
MovieResult result = agentRunResponse2.Result;
DisplayMovies(result);
Utils.Separator();

// More complex process
JsonSerializerOptions jsonSerializerOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    Converters = { new JsonStringEnumConverter()}
};

AIAgent agent3 = client.GetChatClient(LLMConfig.DeploymentOrModelId).CreateAIAgent(instructions: "You are an expert in IMDB movies lists");
ChatResponseFormatJson chatResponseformatJson = Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema<MovieResult>(jsonSerializerOptions);

AgentRunResponse response3 = await agent3.RunAsync(question, options: new ChatClientAgentRunOptions()
{
    ChatOptions = new Microsoft.Extensions.AI.ChatOptions()
    {
        ResponseFormat = chatResponseformatJson
    }
});
MovieResult result3 = response3.Deserialize<MovieResult>(jsonSerializerOptions);
DisplayMovies(result3);
Utils.Separator();

void DisplayMovies(MovieResult result)
{
    if (result.Movies == null || result.Movies.Count == 0)
    {
        Utils.WriteLineWarning("No movies found.");
        return;
    }
    Utils.WriteLineSuccess($"Found {result.Movies.Count} movies:");
    foreach (var movie in result.Movies)
    {
        Console.WriteLine($"  Title: {movie.Title}");
        Console.WriteLine($"  Director: {movie.Director}");
        Console.WriteLine($"  Release Year: {movie.ReleaseYear}");
        Console.WriteLine($"  Rating: {movie.Rating}");
        Console.WriteLine($"  Available on Streaming: {movie.IsAvailableOnStreaming}");
        Console.WriteLine($"  Genre: {movie.Genre}");
        Console.WriteLine($"  Music Composer: {movie.MusicComposer}");
        if (movie.Tags != null && movie.Tags.Count > 0)
        {
            Console.WriteLine($"  Tags: {string.Join(", ", movie.Tags)}");
        }
        Console.WriteLine();
    }
}
