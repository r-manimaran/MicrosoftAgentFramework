using AgentApp_UsingRC2;
using AgentApp_UsingRC2.Models;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using OpenAI.Chat;
using SharedLib;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new AzureKeyCredential(LLMConfig.ApiKey));

const string question = "can you please list top 10 tamil movies in IMDB?";
const string instructions = "You are an expert in IMDB Movie lists";

AIAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId).AsAIAgent(instructions);

Utils.WriteLineInformation("Without Structured Output");
AgentResponse unstructuredResponse = await agent.RunAsync(question);
Console.WriteLine(unstructuredResponse);

Utils.Separator();

AgentResponse<List<Movie>> structuredResponse = await agent.RunAsync<List<Movie>>(question);
List<Movie> movies = structuredResponse.Result;

foreach (Movie movie in movies)
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
