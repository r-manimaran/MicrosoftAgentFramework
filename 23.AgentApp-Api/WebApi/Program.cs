using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Agents.AI;
using Microsoft.OpenApi;
using OpenTelemetry.Trace;
using WebApi;

var builder = WebApplication.CreateBuilder(args);

const string  SourceName = "AiSource.WebApi";

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OpenAPI v1", Version = "v1" });
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource(SourceName);
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddConsoleExporter();
        var aiConnectionString = builder.Configuration.GetConnectionString("ApplicationInsights");
        if (!string.IsNullOrEmpty(aiConnectionString))
        {
            tracing.AddAzureMonitorTraceExporter(options =>
            {
                options.ConnectionString = aiConnectionString;
            });
        }
    });

builder.Services.AddSingleton<AIAgent>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return AIAgentFactory.CreateAIAgent(config);
});



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "OpenAPI v1");
    });
}
else
{
    // If you want Swagger in non-development, enable these lines:
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "OpenAPI v1");
    });
}


app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapPost("/ask", async (AIAgent agent, string question) =>
{
    var response = await agent.RunAsync(question);

    return Results.Ok(new { Answer = response });
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
