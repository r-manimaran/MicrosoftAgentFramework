using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using OpenAI;
using System.ClientModel;

namespace WebApi;

public static class AIAgentFactory
{
    public static AIAgent CreateAIAgent(IConfiguration config)
    {
        var client = new AzureOpenAIClient(
             new Uri(config["AzureOpenAI:Endpoint"]!),
             new ApiKeyCredential(config["AzureOpenAI:ApiKey"]!));
        var deploymentName = config["AzureOpenAI:DeploymentName"]!;

        return client.GetChatClient(deploymentName)
            .CreateAIAgent(
            name: "WebApiAgent",
            instructions: "You are a helpful AI agent integrated into a web API application.")
            .AsBuilder()
            .Use(Middleware.FunctionCallMiddleware)
            .UseOpenTelemetry("AiSource.WebApi", options =>
            {
                options.EnableSensitiveData = true;
            })
           .Build();
    }
}
