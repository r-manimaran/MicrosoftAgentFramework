using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using RagAgentApp;
using RagAgentApp.Models;
using System.Text.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
                        new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));

ChatClient chatClient = client.GetChatClient(LLMConfig.DeploymentOrModelId);

string jsonMovies = await File.ReadAllTextAsync("mymovies.json");

Movie[] moviesDataForRAG = JsonSerializer.Deserialize<Movie[]>(jsonMovies)!;

ChatMessage question = new ChatMessage(ChatRole.User, "List 3 highest rated adventure movies (list their title, plot, year and rating ?");

ChatClientAgent agent = chatClient.CreateAIAgent(instructions: "You are an expert on a set of fictious movies given to you. (don't have any idea about real world movies");

Utils.WriteLineSuccess("Scenario 1: RAG with Azure OpenAI Service");
List<ChatMessage> preLoadedEverythingChatMessages = [
      new(ChatRole.Assistant, "Here are all the movies")
    ];

foreach(Movie movie in moviesDataForRAG)
{
    preLoadedEverythingChatMessages.Add(new ChatMessage(ChatRole.Assistant, movie.GetTitleAndDetails()));
}

preLoadedEverythingChatMessages.Add(question);
AgentRunResponse response1 = await agent.RunAsync(preLoadedEverythingChatMessages);
Console.WriteLine(response1);
response1.Usage.OutputAsInformation();

Console.Clear();