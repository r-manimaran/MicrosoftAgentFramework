using MCPapp.Server;
using MCPapp.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();                           // for weatherTool
builder.Services.AddSingleton<ProductCatalogService>();     // in-memory product store
builder.Services.AddSingleton<OrderService>();              // in-memory order store

// --- MCP Server setup -------------------------------------
// Registers all tools in the assembly, with options to configure session behaviour and access control.
builder.Services.AddMcpServer()
    .WithHttpTransport(options =>
    {
        // -- Configure Session options -----------
        // Called ONCE per new client session. We use it to:
        // 1. Read the X-api-Key header
        // 2. Decide the caller's role (admin / readonly /anonymous)
        // 3. Filter the available tools list accordingly.
    options.Stateless = true;
    options.ConfigureSessionOptions = async (httpContext, mcpOptions, ct) =>
    {
        var apiKey = httpContext.Request.Headers["X-api-Key"].FirstOrDefault() ?? "";
        var role = ResolveRole(apiKey);

        // Store role in the session's Items bag so tools can read it.
        httpContext.Items["role"] = role;

        // Admins get access to all tools, including write tools (create_order, update_stock).
        if (role == "readonly" && mcpOptions.ToolCollection is { } tools)
        {
            // Find te write tools you wnat to remove
            var writeTools = tools
            .Where(t =>t.ProtocolTool.Name.StartsWith("create_") || t.ProtocolTool.Name.StartsWith("update_") 
            || t.ProtocolTool.Name.StartsWith("cancel_"))
            .ToList();

            foreach (var tool in writeTools)
                tools.Remove(tool);

           
        }
        await Task.CompletedTask;
    };

    // Keep per-session ExecutionContext so AsyncLocal<T> set above
    // flows through every tool handler in the same session.
    options.PerSessionExecutionContext = true;
    })
    .WithToolsFromAssembly(typeof(Program).Assembly); // auto-register all tools in this assembly

// Add OpenAPI doc Capability
builder.Services.AddHttpContextAccessor();
builder.Services.AddOpenApi("swagger", o=>
{
    // For swagger.json
    o.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0;
    o.AddDocumentTransformer<McpDocumentTransformer>();
});
builder.Services.AddOpenApi("openapi", o =>
{
    // For openapi.json
    o.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
    o.AddDocumentTransformer<McpDocumentTransformer>();
});

var app = builder.Build();

app.MapGet("/", () => "Hello World from MCP Server!");
app.MapMcp("/mcp"); // Map the MCP server to /mcp endpoint
app.MapOpenApi("/{documentName}.json"); // Map OpenAPI docs to /{documentName}/openapi.json)
app.Run();

// ── Helper ────────────────────────────────────────────────────────────────────
static string ResolveRole(string? apiKey) => apiKey switch
{
    "admin-key-123" => "admin",
    "readonly-key-456" => "readonly",
    _ => "anonymous"
};