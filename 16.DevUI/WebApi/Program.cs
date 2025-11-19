using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
AzureOpenAIClient azureOpenAIClient = new AzureOpenAIClient(
    new Uri(builder.Configuration["AzureOpenAI:Endpoint"]),
    new Azure.AzureKeyCredential(builder.Configuration["AzureOpenAI:ApiKey"])
);


// Register Services needed for the DevUI
builder.Services.AddChatClient(azureOpenAIClient.GetChatClient("gpt-4o-mini").AsIChatClient());
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

// Register a Dummy Agent
builder.AddAIAgent("Comic Book Guy", "You are comic-book guy from South Park")
    .WithAITools(AIFunctionFactory.Create(GetWeather));

//Build a normal agent
string realAgentName = "Real Agent";
AIAgent myAgent = azureOpenAIClient.GetChatClient("gpt-4o-mini")
    .CreateAIAgent(name: realAgentName, instructions: "Speak like a pirate", tools: [AIFunctionFactory.Create(GetWeather)]);

builder.AddAIAgent(realAgentName, (serviceProvider, key) => myAgent); // Get registred as a Keyed singleton so name on real agent and key must match

// Register a Sample workflow
IHostedAgentBuilder frenchTranslator = builder.AddAIAgent("french-translator", "Translate any text you get to French");
IHostedAgentBuilder germanTranslator = builder.AddAIAgent("german-translator", "Translate any text you get to German");
IHostedAgentBuilder tamilTranslator = builder.AddAIAgent("tamil-translator", "Translate any text you get to Tamil");
builder.AddWorkflow("translation-workflow-sequential", (sp, key) =>
{
    IEnumerable<AIAgent> agentsForWorkflow = new List<IHostedAgentBuilder>() { frenchTranslator, germanTranslator, tamilTranslator }
    .Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
    return AgentWorkflowBuilder.BuildSequential(workflowName: key, agents: agentsForWorkflow);
}).AddAsAIAgent();

builder.AddWorkflow("translation-workflow-concurrent", (sp, key) =>
{
    IEnumerable<AIAgent> agentsForWorkflow = new List<IHostedAgentBuilder>() { frenchTranslator, germanTranslator, tamilTranslator }
    .Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));

    return AgentWorkflowBuilder.BuildConcurrent(workflowName: key, agents: agentsForWorkflow);
}).AddAsAIAgent("Concurrent Workflow");

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapOpenAIResponses();
    app.MapOpenAIConversations();
    app.MapDevUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


static string GetWeather(string city)
{
    string[] _Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
    int temp = Random.Shared.Next(-20, 55);
    string summary = _Summaries[Random.Shared.Next(_Summaries.Length)];
    return $"The weather in {city} is {summary} with a temperature of {temp} degrees Celsius.";
}
