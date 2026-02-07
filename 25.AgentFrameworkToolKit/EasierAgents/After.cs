using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.AzureOpenAI;
using Microsoft.Extensions.AI;
using AgentFrameworkToolkit;
namespace EasierAgents;

public class After
{
    public static async Task RunAsync()
    {
        AzureOpenAIAgentFactory agentFactory = new(LLMConfig.Endpoint, LLMConfig.ApiKey);

        AzureOpenAIAgent agent = agentFactory.CreateAgent(new AgentOptions
        {
            Model = OpenAIChatModels.Gpt5Mini,
            ReasoningEffort = OpenAIReasoningEffort.Low,
            Tools = [AIFunctionFactory.Create(WeatherTool.GetWeather)],
            RawToolCallDetails = details => { Utils.WriteLineWarning(details.ToString()); }
        });

        Microsoft.Agents.AI.ChatClientAgentResponse<WeatherReport> response = await agent.RunAsync<WeatherReport>("What is the weather like in Chennai");
        WeatherReport weatherReport = response.Result;
        Console.WriteLine("City:"+ weatherReport.City);
        Console.WriteLine("Condition:" + weatherReport.Condition);
        Console.WriteLine("CiDegreesty:" + weatherReport.Degrees);
        Console.WriteLine("Fahrenheit:" + weatherReport.Fahrenheit);
        response.Usage.OutputAsInformation();

    }
}
