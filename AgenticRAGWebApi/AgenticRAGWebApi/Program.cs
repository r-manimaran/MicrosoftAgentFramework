using AgenticRAGWebApi.Agents;
using AgenticRAGWebApi.Data;
using AgenticRAGWebApi.Repositories;
using AgenticRAGWebApi.Services;
using AgenticRAGWebApi.Tools;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Qdrant.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton(_ => new QdrantClient(builder.Configuration["Qdrant:Host"]!, 6334));

builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
new AzureOpenAIClient(
    new Uri(builder.Configuration["AzureOpenAI:Endpoint"]!),
    new DefaultAzureCredential())
    .AsEmbeddingGenerator("text-embedding-3-small"));

builder.Services.AddDbContextFactory<ITSupportDbContext>(options =>
options.UseSqlServer(
    builder.Configuration.GetConnectionString("ITSupport"),
    sql =>
    {
        sql.EnableRetryOnFailure(maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
        sql.CommandTimeout(30);
    }
    ));

builder.Services.AddSingleton<HybridSearchService>();
builder.Services.AddSingleton<AgentTools>();
builder.Services.AddSingleton<ITSupportAgentFactory>();
//builder.Services.AddSingleton<ITSupportService>
builder.Services.AddScoped<ITicketRepository, TicketRepository>();

//Named HttpClient for ServiceNow
builder.Services.AddHttpClient("ServiceNow", c => c.BaseAddress = new Uri(builder.Configuration["ServiceNow:BaseUrl"]!));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

