using McpCliClient.Auth;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;


Console.WriteLine("=== MCP CLI Client ===\n");

var keycloakBase = args.FirstOrDefault() ?? "http://localhost:8081";
var mcpBase = args.ElementAtOrDefault(1) ?? "http://localhost:5037";

// 1. Acquire token via Device Authorization Flow
var http = new HttpClient();
var flow = new DeviceAuthFlow(http, keycloakBase);
var token = await flow.AcquireTokenAsync();

// ── DIAGNOSTIC 1: Confirm token looks valid ───────────────────────────────────
Console.WriteLine($"\n[DIAG] Token acquired. Length: {token.Length}");
Console.WriteLine($"[DIAG] Token prefix: {token[..Math.Min(40, token.Length)]}...");
Console.WriteLine($"[DIAG] MCP endpoint: {mcpBase}/mcp\n");

// ── STEP 2: Manually probe the /mcp endpoint with curl-style test ────────────
Console.WriteLine("[DIAG] Testing /mcp endpoint directly with HttpClient...");
Console.WriteLine(token);


// 2. Build an HttpClient that injects the Bearer token on every request
var authedHttpClient = new HttpClient();
authedHttpClient.DefaultRequestHeaders.Authorization =
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

// 3. Create the transport — HttpClientTransport is the correct 1.x type
//    Pass the pre-authed HttpClient into the overload that accepts one

var transport = new HttpClientTransport(
    new HttpClientTransportOptions
    {
        Endpoint = new Uri($"{mcpBase}/mcp"),
        Name = "MCP Keycloak CLI",
        TransportMode = HttpTransportMode.StreamableHttp
    },
    authedHttpClient,   // <-- token flows through this client automatically,
    ownsHttpClient: false
);

//// 4. Create the MCP client — McpClient.CreateAsync, NOT McpClientFactory
await using McpClient mcpClient = await McpClient.CreateAsync(
    transport,
    new McpClientOptions
    {
        ClientInfo = new() { Name = "McpKeycloakCLI", Version = "1.0.0" }
    }
);

// 3. List available tools
Console.WriteLine("Available tools:");
try
{
    var tools = await mcpClient.ListToolsAsync();
    if (!tools.Any())
    {
        Console.WriteLine("[DIAG] ListToolsAsync succeeded but returned 0 tools.");
    }
    foreach (var tool in tools)
        Console.WriteLine($"  - {tool.Name}: {tool.Description}");
}
catch(McpProtocolException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[ERROR] MCP protocol error: {ex.ToString()}");
    Console.WriteLine($"[ERROR] Error code: {ex.ErrorCode}");
    Console.WriteLine($"[ERROR] Error message: {ex.InnerException.StackTrace.ToString()}");
    Console.WriteLine($"[ERROR] Error data: {ex.Data}");
    Console.ResetColor();
}
// 4. Interactive REPL
Console.WriteLine("\nEnter tool calls (or 'exit' to quit):");
Console.WriteLine("  Example: weather London\n");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(input) || input == "exit") break;

    var parts = input.Split(' ', 2);
    var name = parts[0];
    var arg = parts.ElementAtOrDefault(1) ?? "";

    try
    {
        var args2 = name.ToLower() switch
        {
            "weather" => new Dictionary<string, object?> { ["city"] = arg },
            "sysinfo" => new Dictionary<string, object?>(),
            _ => new Dictionary<string, object?>()
        };
        Console.WriteLine($"Selected:{name}");

        string toolName = name.ToLower() switch
        {
            "weather" => "get_weather",
            "sysinfo" => "get_system_info",
            _ => name
        };

        var result = await mcpClient.CallToolAsync(toolName, args2);
        Console.ForegroundColor = ConsoleColor.Green;
        var text = ((TextContentBlock)result.Content.FirstOrDefault()!).Text;
        Console.WriteLine($"{text}");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  Error: {ex.Message}");
        Console.ResetColor();
    }
}