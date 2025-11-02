using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using OpenAI;

namespace AIAgentRAG;

public static class Option3CommonSense
{
    public static async Task Run(bool importData, Models.Movie[] movieDataForRag, ChatMessage question, AzureOpenAIClient client,
                          SqlServerCollection<Guid, MovieVectorStoreRecord> collection)
    {
        if (importData)
        {
            await EnhanceDataEmbedding.Embed(client, collection, movieDataForRag);
        }

        ChatClientAgent intentAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
                                .CreateAIAgent("You are good at infering intent from user question");
        ChatClientAgentRunResponse<IntentResponse> intentResponse = await intentAgent.RunAsync<IntentResponse>(question);
        IntentResponse intent = intentResponse.Result;
        switch (intent.TypeOfQuestion)
        {
            case TypeOfQuestion.MovieGenreRanking:
                {
                    List<MovieVectorStoreRecord> matchingMovies = [];
                    await foreach (var record in collection.GetAsync(record => record.Genre == intent.Genre, int.MaxValue))
                    {
                        matchingMovies.Add(record);
                    }

                    MovieVectorStoreRecord[] topMovies = matchingMovies.OrderByDescending(x => x.Rating).Take(intent.NumberOfResults).ToArray();

                    AIAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
                        .CreateAIAgent(instructions: $"""
                    You are an expert a set of made up movies given to you (aka don't consider movies from your world-knolwedge)
                    YOu are given the data for the user's question '{question.Text}' and need to present it as if you did the answer
                    """);
                    AgentRunResponse response = await agent.RunAsync(string.Join(";", topMovies.Select(x => x.GetTitleAndDetails())));
                    Console.WriteLine(response);
                    response.Usage.OutputAsInformation();                    
                }
                break;
            case TypeOfQuestion.MovieGenreSearch:
                {
                    EnhancedSearchTool searchTool = new(collection);
                    AIAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
                        .CreateAIAgent(instructions: """
                        YOu are an expert on a set of made up movies given to you (aka don't consider movies from your world-knolwedge)
                        when using tools use keywords only based on the users question so it is better for similary search. when listing the 
                        movies (list their titles, plots, ratings, genre and year)
                        """,
                        tools: [AIFunctionFactory.Create(searchTool.SearchVectorSearch)])
                        .AsBuilder()
                        .Use(Middleware.FunctionCallMiddleware)
                        .Build();
                    AgentRunResponse response = await agent.RunAsync(question);
                    Console.WriteLine(response);
                    response.Usage?.OutputAsInformation();                        
                }
                break;
            case TypeOfQuestion.GenericMovieQuestion:
                {
                    OriginalSearchTool searchTool = new OriginalSearchTool(collection);

                    AIAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
                        .CreateAIAgent(instructions: """"
                        YOu are an expert on a set of made up movies given to you (aka don't consider movies from your world-knolwedge)
                        when using tools use keywords only based on the users question so it is better for similary search. when listing the 
                        movies (list their titles, plots, ratings, genre and year)                        
                        """",
                        tools: [AIFunctionFactory.Create(searchTool.SearchVectorStore)])
                        .AsBuilder()
                        .Build();
                    AgentRunResponse response = await agent.RunAsync(question);
                    Console.WriteLine(response);
                    response.Usage?.OutputAsInformation();
                }
                break;
        }
                              
    }
}
