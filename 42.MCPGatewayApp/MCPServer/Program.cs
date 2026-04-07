using MCPServer.Data;
using MCPServer.Tools;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Tickets") ?? "Data Source=tickets.db";

builder.Services.AddDbContextFactory<TicketDbContext>(options =>
{
    if (connectionString.StartsWith("Data Source="))
        options.UseSqlite(connectionString);
    else
        options.UseSqlServer(connectionString);

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging().EnableDetailedErrors();
    }
});

builder.Services.AddScoped<TicketRepository>();

// -- Add MCP Server Tools
builder.Services.AddMcpServer().WithHttpTransport().WithTools<IncidentTools>();

// -- Add health checks
builder.Services.AddHealthChecks();//.AddDbContextCheck<TicketDbContext>();

// --- Logging
builder.Logging.AddConsole();

var app = builder.Build();

// -- Apply the database migrations on startup
using(var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
    await db.Database.EnsureCreatedAsync();   // creates schema + seed data
    app.Logger.LogInformation("Database ready. Connection: {Conn}", connectionString);
}

// -- Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

// -- Map the MCP endpoints
app.MapMcp("/mcp");

app.MapHealthChecks("/health");

app.Logger.LogInformation(
    "MCP server started. Tools: get_open_incidents, get_ticket_detail, update_ticket_status");
app.Logger.LogInformation(
    "Test with MCP Inspector: npx @modelcontextprotocol/inspector http://localhost:5100/mcp");

await app.RunAsync();

