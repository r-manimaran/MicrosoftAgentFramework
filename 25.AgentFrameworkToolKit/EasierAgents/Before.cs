using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;
namespace EasierAgents;

public class Before
{
    public static async Task RunAsync()
    {
        AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new ApiKeyCredential(LLMConfig.ApiKey));

        AIAgent agent = client.
            GetChatClient("gpt-5-mini")
            .AsAIAgent(options: new ChatClientAgentOptions
            {
                ChatOptions = new Microsoft.Extensions.AI.ChatOptions
                {
                    RawRepresentationFactory = _ => new ChatCompletionOptions
                    {
#pragma warning disable OPENAI001
                        ReasoningEffortLevel = "low"
#pragma warning restore OPENAI001
                    },
                    Tools = [AIFunctionFactory.Create(WeatherTool.GetWeather)]
                }
            })
            .AsBuilder()
            .Use(FunctionCallMiddleware)
            .Build();

        JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            Converters = { new JsonStringEnumConverter() }
        };

        AgentResponse response = await agent.RunAsync("What is the weather in Seattle?",
            options: new ChatClientAgentRunOptions()
            {
                ChatOptions = new ChatOptions
                {
                    ResponseFormat = ChatResponseFormat.ForJsonSchema<WeatherReport>(jsonSerializerOptions)
                }
            });
        WeatherReport weatherReport = response.Deserialize<WeatherReport>(jsonSerializerOptions);
        Console.WriteLine("City: " + weatherReport.City);
        Console.WriteLine("Condition: " + weatherReport.Condition);
        Console.WriteLine("Degrees: " + weatherReport.Degrees);
        Console.WriteLine("Fahrenheit: " + weatherReport.Fahrenheit);
        response.Usage.OutputAsInformation();
    }

    static async ValueTask<object?> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
    {
        StringBuilder functionCallDetails = new();
        functionCallDetails.Append($"- Tool Call: '{context.Function.Name}'");
        if (context.Arguments.Count > 0)
        {
            functionCallDetails.Append($" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))}");
        }

        Utils.WriteLineInformation(functionCallDetails.ToString());

        return await next(context, cancellationToken);
    }
}
