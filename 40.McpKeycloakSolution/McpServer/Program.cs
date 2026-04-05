using McpServer.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

// -- KeyCloak OIDC JWT authentication
// Aspire injects the keycloak base URL via Environment variable
var keycloakBase = builder.Configuration["services__keycloak__http__0"] ?? "http://localhost:8081";
var realm = "mcp-realm";
var authority = $"{keycloakBase}/realms/{realm}";
Console.WriteLine(authority);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = "account";   // Keycloak default audience
            options.RequireHttpsMetadata = false; // dev only - remove in prod
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidIssuer = authority,
                ValidateAudience = false, // Keycloak tokens use 'account' — relax for dev
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
            options.Events = new KeycloakJwtEvents();  // custom role-mapping events
        });


builder.Services.AddAuthorization(options =>
{
    // Gateway policy - mcp-user or mcp-admin can connect    
    //options.AddPolicy("McpUser", p => p.RequireRole("mcp-user"));
    options.AddPolicy("McpUser", p => p.RequireAssertion(ctx => ctx.User.IsInRole("mcp-user") ||
    ctx.User.IsInRole("mcp-admin")));

    // Elevated policy - only mcp-admin can access certain tools
    options.AddPolicy("McpAdmin", p => p.RequireRole("mcp-admin"));
});


// -- MCP Streamable HTTP ---
builder.Services
        .AddMcpServer()
        .WithHttpTransport()
        .WithToolsFromAssembly() // scans for [McpTool] classes
        .AddAuthorizationFilters();
//o =>
//{
//    o.Stateless = false;
//    o.ConfigureSessionOptions = async (ctx, mcpOpt, ct) =>
//    {
//        string toolOption = ctx.Request.Query["option"].ToString();
//        mcpOpt.ToolCollection = [];

//    };
//}
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

//app.UseHttpsRedirection();

app.MapMcp("/mcp").RequireAuthorization("McpUser");

app.Use(async (context, next) =>
{
    try
    {
        Console.WriteLine($"[DIAG] Incoming request: {context.Request.Method} {context.Request.Path}");
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices
            .GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "[MCP SERVER] Unhandled exception on {Method} {Path}",
            context.Request.Method, context.Request.Path);
        throw;
    }
});

app.Run();

