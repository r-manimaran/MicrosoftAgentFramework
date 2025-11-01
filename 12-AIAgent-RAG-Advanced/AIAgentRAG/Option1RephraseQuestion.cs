﻿using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIAgentRAG;

public static class Option1RephraseQuestion
{
    public static async Task Run(bool importData, Models.Movie[] movieDataForRag, ChatMessage question, AzureOpenAIClient client, 
        SqlServerCollection<Guid, MovieVectorStoreRecord> collection)
    {
        if (importData)
        {
            await OriginalEmbedding.Embed(collection, movieDataForRag);
        }
        OriginalSearchTool searchTool = new(collection);

        AIAgent agent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
            .CreateAIAgent(
            instructions: """
                          You are an exper a set of made up movies given to you (aka don't consider movies from your world-knowledge)
                          When using tools use keywords only based on the users question so it is better for similarity search
                          When listing the movies (list their titles, plots and ratings)
                          """,
            tools: [AIFunctionFactory.Create(searchTool.SearchVectorStore)])
            .AsBuilder()
            .Use(Middleware.FunctionCallMiddleware)
            .Build();

        AgentRunResponse response = await agent.RunAsync(question);
        Console.WriteLine(response);
        response.Usage?.OutputAsInformation();

    }
}


