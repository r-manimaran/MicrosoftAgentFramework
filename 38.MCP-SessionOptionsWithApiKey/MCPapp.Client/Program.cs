using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("mcp-server", (sp,client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    // static key baked in at startup
    //client.DefaultRequestHeaders.Add("X-Api-Key", "admin-key-123");
    client.DefaultRequestHeaders.Add("X-Api-Key", config["McpServer:ApiKey"]);
});

builder.Services.AddSingleton<IClientTransport>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("mcp-server");

    return new HttpClientTransport(
        new HttpClientTransportOptions
        {
           // Endpoint = new Uri("https://localhost:7249/mcp"),
            Endpoint = new Uri(config["McpServer:Url"]!),
            //AdditionalHeaders = new Dictionary<string, string>
            //{
            //    // readonly-key-456
            //    ["X-Api-Key"] = "admin-key-123"
            //}
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


app.MapGet("/convertCurrency", async Task<IResult> (Task<McpClient> mcpClientTask,
   [FromBody] CurrencyConverter request) =>
{
    var mcpClient = await mcpClientTask;
    var result = await mcpClient.CallToolAsync("convert_currency", new Dictionary<string, object?>
    {
        //["from"] = "USD",
        //["to"] = "EUR",
        //["amount"] = 100m

         ["from"] = request.From,
        ["to"] = request.To,
        ["amount"] = request.Amount
    });
    if(result.IsError is not true)
    {
        if(result.Content.Count > 0)
        {
            ContentBlock cb = result.Content[0];
            var text = ((TextContentBlock)result.Content[0]).Text;
            var data = JsonSerializer.Deserialize<JsonElement>(text);
            return TypedResults.Ok(data);
            //var text = ((TextContentBlock)cb).Text;
            //var data = JsonSerializer.Deserialize<CurrencyResult>(text);
            //return TypedResults.Ok(data); // serializes cleanly as JSON object


         }
    }
    return TypedResults.BadRequest();
});

app.MapGet("/", () => "Hello World!");

app.Run();

record CurrencyConverter(string From, string To, decimal Amount);
public record CurrencyResult(
    string From,
    string To,
    decimal OriginalAmount,
    decimal ConvertedAmount,
    decimal Rate,
    string Date);