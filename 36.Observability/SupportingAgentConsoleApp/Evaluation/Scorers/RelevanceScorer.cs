using Azure.AI.OpenAI;
using OpenAI.Chat;
using SupportingAgentConsoleApp.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SupportingAgentConsoleApp.Evaluation.Scorers;

public class RelevanceScorer    
{
    private readonly ChatClient _judgeClient;
    public RelevanceScorer(string endpoint, string apiKey, string judgeModel)
    {
        _judgeClient = new AzureOpenAIClient(new Uri(endpoint),
                new System.ClientModel.ApiKeyCredential(apiKey))
            .GetChatClient(judgeModel);

    }

    public async Task<double> ScoreAsync(string userQuery, string agentResponse)
    {
        var prompt = $"""
                    You are an evaluation judge. Score the response's relevance to the query on a scale of 0.0 to 1.0.
                    Respond ONLY with a JSON object like: "score": <float>, "reason": "<one sentence>"
                    Query: {userQuery}
                    Response: {agentResponse}
                    """;

        var result = await _judgeClient.CompleteChatAsync(
            [new UserChatMessage(prompt)],
            new ChatCompletionOptions
            {
                MaxOutputTokenCount = 100,
                Temperature = 0
            });
        var json = result.Value.Content[0].Text;
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("score").GetDouble();
    }
}
