using ModelContextProtocol.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("mcp-server", client =>
{
    // static key baked in at startup
    client.DefaultRequestHeaders.Add("X-Api-Key", "admin-key-123");
});

builder.Services.AddSingleton<IClientTransport>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("mcp-server");

    return new HttpClientTransport(
        new HttpClientTransportOptions
        {
            Endpoint = new Uri("https://localhost:7249/mcp"),
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["X-Api-Key"] = "readonly-key-456"
            }
        },
        httpClient,
        ownsHttpClient: false);
});

builder.Services.AddSingleton<Task<McpClient>>(sp =>
{
    var transport = sp.GetRequiredService<IClientTransport>();
    return McpClient.CreateAsync(transport);
});

//var transport = new HttpClientTransport(new HttpClientTransportOptions
//{
//    Endpoint = new Uri("https://localhost:xxxx")
//});
//builder.Services.AddTransient((sp) => { return McpClient.CreateAsync(transport); });

var app = builder.Build();

app.MapGet("/getTools", async Task<IResult> (Task<McpClient> mcpClientTask) =>
{
    var mcpClient = await mcpClientTask;

    var tools = await mcpClient.ListToolsAsync();

    return TypedResults.Ok(tools);
});

app.MapGet("/", () => "Hello World!");

app.Run();
