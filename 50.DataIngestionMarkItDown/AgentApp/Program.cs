using AgentApp;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using System.ClientModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.DataIngestion.Chunkers;
using Microsoft.ML.Tokenizers;

Console.WriteLine("Starting the application...");
string connectionString = LLMConfig.SqlConnectionString;


var azureClient = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),new ApiKeyCredential(LLMConfig.ApiKey));

var embeddingGenerator = azureClient.GetEmbeddingClient(LLMConfig.EmbeddingModelName).AsIEmbeddingGenerator();

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Debug);
    })
    .ConfigureServices(s =>
    {
        s.AddSqlServerVectorStore(o => connectionString,
            o => new SqlServerVectorStoreOptions()
            {
                EmbeddingGenerator = embeddingGenerator,
            });
    }).Build();

SqlServerVectorStore? vectorStore = host.Services.GetRequiredService<SqlServerVectorStore>();


VectorStoreWriter<string> writer = new(vectorStore, dimensionCount: 1536, new VectorStoreWriterOptions());

IngestionDocumentReader reader = new MarkItDownMcpReader(new Uri("http://localhost:3001/mcp"));

using IngestionPipeline<string> pipeline = new IngestionPipeline<string>(reader, GetChunker(embeddingGenerator), writer);
await foreach(var r in pipeline.ProcessAsync(new DirectoryInfo(@"C:\Maran\Study\Documents\DataIngestion"), searchPattern:"*.pdf")){
    Console.WriteLine($"Result:{r.Succeeded}");
}

static IngestionChunker<string> GetChunker(IEmbeddingGenerator<string,Embedding<float>> embeddingGenerator)
    {
        var co = new IngestionChunkerOptions(TiktokenTokenizer.CreateForModel("gpt-4o-mini"));
        return new SemanticSimilarityChunker(embeddingGenerator, co);
    }
