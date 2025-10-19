using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MultiAgent;
using MultiAgent.Extensions;
using MultiAgent.Tools;
using OpenAI;
using SharedLib;
using System.Text;

AzureOpenAIClient client = new AzureOpenAIClient(
    new Uri(LLMConfig.Endpoint),
    new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));

AIAgent stringAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
                            .CreateAIAgent(name:"StringAgent",
                            instructions:" You are a string manipulator",
                            tools: [
                                AIFunctionFactory.Create(StringTools.Reverse),
                                //AIFunctionFactory.Create(StringTools.Reverse),
                                //AIFunctionFactory.Create(StringTools.Reverse),
                                //AIFunctionFactory.Create(StringTools.Reverse),
                                //AIFunctionFactory.Create(StringTools.Reverse),
                                //AIFunctionFactory.Create(StringTools.Reverse),
                                //AIFunctionFactory.Create(StringTools.Reverse),
                                //AIFunctionFactory.Create(StringTools.Reverse),
                                //AIFunctionFactory.Create(StringTools.Reverse),
                                //AIFunctionFactory.Create(StringTools.Reverse),
                                //AIFunctionFactory.Create(StringTools.Reverse),
                                //AIFunctionFactory.Create(StringTools.Reverse),
                                //AIFunctionFactory.Create(StringTools.Reverse),
                                //AIFunctionFactory.Create(StringTools.Reverse),
                                AIFunctionFactory.Create(StringTools.UpperCase),
                                AIFunctionFactory.Create(StringTools.LowerCase)
                                ])
                            .AsBuilder()
                            .Use(FunctionCallMiddleware)
                            .Build();

// Agent 2:
AIAgent numericAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
                        .CreateAIAgent(name:"NumberAgent",
                    instructions:" You are a number expert",
                    tools: [
                        AIFunctionFactory.Create(NumericTools.Add),
                        AIFunctionFactory.Create(NumericTools.Subtract),
                        AIFunctionFactory.Create(NumericTools.Multiply),
                        AIFunctionFactory.Create(NumericTools.Divide),
                        //AIFunctionFactory.Create(NumericTools.RandomNumber),
                        //AIFunctionFactory.Create(NumericTools.RandomNumber),
                        //AIFunctionFactory.Create(NumericTools.RandomNumber),
                        //AIFunctionFactory.Create(NumericTools.RandomNumber),
                        //AIFunctionFactory.Create(NumericTools.RandomNumber),
                        //AIFunctionFactory.Create(NumericTools.RandomNumber),
                        //AIFunctionFactory.Create(NumericTools.RandomNumber),
                        AIFunctionFactory.Create(NumericTools.RandomNumber),
                        AIFunctionFactory.Create(NumericTools.AnswerToEveythingNumber)
                        ])
                    .AsBuilder()
                    .Use(FunctionCallMiddleware)
                    .Build();

Utils.WriteLineSuccess("DELEGATE AGENT");

AIAgent delegationAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .CreateAIAgent(
    name: "DelegateAgent",
    instructions: " You are a delegate agent who delegates tasks about string and Numbers to specialized agents. Never does such work yourself ",
    tools: [
            stringAgent.AsAIFunction(new AIFunctionFactoryOptions {
                    Name="StringAgentAsTool"
                    }),
            numericAgent.AsAIFunction(new AIFunctionFactoryOptions {
                Name ="NumberAgentAsTool"
                })
        ])
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();


// Call the delegation agent
// "Uppercase 'Hello world' and add 10 to 32. What is the answer to everything?"
AgentRunResponse response = await delegationAgent.RunAsync("Uppercase 'Hello world'");
Console.WriteLine(response);
Utils.WriteLineInformation("************************************");
Utils.WriteLineInformation($"- Input Tokens:{response.Usage?.InputTokenCount}");
Utils.WriteLineInformation($"- Output Tokens:{response.Usage?.OutputTokenCount}" +
    $"({response.Usage?.GetOutputTokensUsedForReasoning()} was used for reasoning)");

Utils.Separator();

Utils.WriteLineSuccess("JACK OF ALL TRADES AGENT");
AIAgent jackOfAllTradesAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .CreateAIAgent(
    name: "JackOfAllTradesAgent",
    instructions: " You are a jack of all trades agent who can do string and number manipulation. Use the tools as needed",
    tools: [
            AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            //AIFunctionFactory.Create(StringTools.Reverse),
            AIFunctionFactory.Create(StringTools.UpperCase),
            //AIFunctionFactory.Create(StringTools.UpperCase),
            //AIFunctionFactory.Create(StringTools.UpperCase),
            //AIFunctionFactory.Create(StringTools.UpperCase),
            //AIFunctionFactory.Create(StringTools.UpperCase),
            //AIFunctionFactory.Create(StringTools.UpperCase),
            AIFunctionFactory.Create(StringTools.LowerCase),
            AIFunctionFactory.Create(NumericTools.Add),
            AIFunctionFactory.Create(NumericTools.Subtract),
            AIFunctionFactory.Create(NumericTools.Multiply),
            AIFunctionFactory.Create(NumericTools.Divide),
            AIFunctionFactory.Create(NumericTools.RandomNumber),
            AIFunctionFactory.Create(NumericTools.AnswerToEveythingNumber)
        ])
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();

AgentRunResponse response2 = await jackOfAllTradesAgent.RunAsync("Uppercase 'Hello world'");
Console.WriteLine(response2);
Utils.WriteLineInformation("************************************");
Utils.WriteLineInformation($"- Input Tokens:{response2.Usage?.InputTokenCount}");
Utils.WriteLineInformation($"- Output Tokens:{response2.Usage?.OutputTokenCount}" +
    $"({response2.Usage?.GetOutputTokensUsedForReasoning()} was used for reasoning)");

Utils.Separator();


async ValueTask<object?> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken,ValueTask<object?>> next,
    CancellationToken cancellationToken = default)
{
    StringBuilder functionCallDetails = new();
    functionCallDetails.Append($" - Tool call :'{context.Function.Name}' [Agent:{callingAgent.Name}]");
    if(context.Arguments.Count >0)
    {
        functionCallDetails.Append(" with args: ");
        foreach (var arg in context.Arguments)
        {
            functionCallDetails.Append($" {arg.Key}='{arg.Value}' ");
        }
    }
    Utils.WriteLineInformation(functionCallDetails.ToString());
    return await next(context, cancellationToken);
}