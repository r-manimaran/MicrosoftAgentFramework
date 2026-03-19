
using AgentApp;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.ComponentModel;

// -------------------------------------------------------
// Step 1: Define a simple function tool for WeatherAgent
// -------------------------------------------------------
[Description("Gets the current weather for a given city.")]
static string GetWeather(
    [Description("The city to get weather for.")] string city)
{
    // Simulated weather data (replace with real API call)
    return city.ToLower() switch
    {
        "amsterdam" => "Cloudy, 12°C, light rain expected.",
        "paris" => "Sunny, 18°C, pleasant breeze.",
        "tokyo" => "Clear skies, 22°C, humidity 60%.",
        _ => $"Weather data not available for {city}."
    };
}

// -------------------------------------------------------
// Step 2: Create the INNER agent (WeatherAgent)
//         - Has its own tool: GetWeather function
// -------------------------------------------------------

var azureClient = new AzureOpenAIClient(
    new Uri(LLMConfig.Endpoint), new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));

AIAgent weatherAgent = azureClient.GetChatClient("gpt-4o-mini")
    .AsAIAgent(
    name: "WeatherAgent",
    description: "An agent that provides current weather information for a city.",
    instructions: "You are a weather assistant. Use the GetWeather tool to answer weather questions.",
    tools: [AIFunctionFactory.Create(GetWeather)]
    );

// -------------------------------------------------------
// Step 3: Create the OUTER agent (TravelPlannerAgent)
//         - Uses WeatherAgent as a TOOL via .AsAIFunction()
// -------------------------------------------------------
AIAgent travelPlannerAgent = azureClient
    .GetChatClient("gpt-4o-mini")
    .AsAIAgent(
        name: "TravelPlannerAgent",
        instructions: """
            You are a travel planner. When asked about a destination,
            call the WeatherAgent tool to get current weather,
            then give packing and activity recommendations based on it.
            """,
        tools: [weatherAgent.AsAIFunction()]  // ← Agent as a tool!
    ).AsBuilder().Use(LogFunctionNameMiddleware).Build();
// -------------------------------------------------------
// Step 4: Run the outer agent
// -------------------------------------------------------
Console.WriteLine("=== Travel Planner Agent ===\n");
Console.WriteLine(
    await travelPlannerAgent.RunAsync(
        "I'm travelling to paris tomorrow. What should I pack?")
);
Console.ReadLine();

async static ValueTask<object?> LogFunctionNameMiddleware(AIAgent agent, FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>>next, CancellationToken cancellationToken)
{
    
    Console.WriteLine($"LogFunctionNameMiddleware- function invoked:{context.Function.Name}");

    return await next(context, cancellationToken);
}