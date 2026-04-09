using McpGateway.Core.Client;
using McpGateway.Core.Interfaces;
using McpGateway.Core.Middlewares;
using McpGateway.Core.Models;
using McpGateway.Core.Pipeline;
using McpGateway.Core.Registry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// -- Mcp Servers registrations are declared in appsettings.json under McpServers.
// The gateway will read them at startup - no code changes needed to add/remove servers or tools, just update the config and restart.
var serverRegistrations = builder.Configuration.GetSection("McpServers")
                        .Get<List<McpServerRegistration>>() 
                        ?? new List<McpServerRegistration>();

if(serverRegistrations.Count == 0)
{
    Console.WriteLine("No MCP servers configured. Please add server registrations to appsettings.json under the McpServers section.");
}

// Server Registry
builder.Services.AddSingleton<IMcpServerRegistry>(
        new InMemoryMcpServerRegistry(serverRegistrations));

// -- Named HttpClient per registered MCP Server
// Each server gets its own HttpClient with its own base address and timeout.
// This allows per-server tuning without changing the generic client code.

foreach (var server in serverRegistrations)
{
    builder.Services.AddHttpClient($"mcp-{server.ServerId}", client =>
    {
        client.BaseAddress = new Uri(server.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(server.TimeoutSeconds);
        client.DefaultRequestHeaders.Add("X-Gateway-source", "McpGateway/1.0");
    });
}

builder.Services.AddSingleton<IMcpServerClient, McpHttpServerClient>();

// -- Middleware pipeline
builder.Services.AddSingleton<IMcpMiddleware, ServerRoutingMiddleware>();
builder.Services.AddSingleton<IMcpMiddleware, McpDispatchMiddleware>();

// Gateway Pipeline
builder.Services.AddSingleton<IMcpGateway, McpGatewayPipeline>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("MCP Gateway started — Milestone 3 (bare routing, no auth)");
logger.LogInformation("Registered servers: {Servers}",
    string.Join(", ", serverRegistrations.Select(s => $"{s.ServerId} → {s.BaseUrl}")));
logger.LogInformation("Swagger UI: http://localhost:5000/swagger");
logger.LogInformation("Health:     http://localhost:5000/health");

await app.RunAsync();

// Needed for WebApplicationFactory in integration tests
public partial class Program { }