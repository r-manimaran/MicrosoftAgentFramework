using Azure.AI.OpenAI;
using WebApiMCP;

var builder = WebApplication.CreateBuilder(args);

AzureOpenAIClient client = new(new Uri(LLMConfig.Endpoint),
    new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));

builder.Services.AddSingleton(client);

builder.Services.AddOpenApi();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
const string expectedApiKey = "xyz-123";

app.MapMcp("/mcp").AddEndpointFilter(async (context,next)=>
{
    if (!string.Equals(context.HttpContext.Request.Headers["x-api-key"], expectedApiKey,StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }
    return await next(context);
});

//app.UseHttpsRedirection();

app.Run();

