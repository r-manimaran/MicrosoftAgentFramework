using AgentAppWebApi.Config;
using Azure.AI.OpenAI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

// --- Configuration
var azureConfig = builder.Configuration.GetSection("AzureOpenAI").Get<AzureOpenAIConfig>()!;


// Azure OpenAI
var azureClient = new AzureOpenAIClient(new Uri(azureConfig.Endpoint),
    new System.ClientModel.ApiKeyCredential(azureConfig.ApiKey));
var app = builder.Build();

app.MapDefaultEndpoints();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

