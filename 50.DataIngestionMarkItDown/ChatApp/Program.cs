
// Configuration
using Azure.AI.OpenAI;
using ChatApp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using System.ClientModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

const int TopK = 5;
const int MaxHistoryTurns = 10;
const float MinRelevance = 0.65f;
string connectionString = LLMConfig.SqlConnectionString;


var azureClient = new AzureOpenAIClient(
    new Uri(LLMConfig.Endpoint),
    new ApiKeyCredential(LLMConfig.ApiKey));

var embeddingGenerator = azureClient
    .GetEmbeddingClient(LLMConfig.EmbeddingModelName)
    .AsIEmbeddingGenerator();

var chatClient = azureClient
    .GetChatClient(LLMConfig.DeploymentOrModelId)
    .AsIChatClient();

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Warning); // quieter than ingestion
    })
    .ConfigureServices(s =>
    {
        s.AddSqlServerVectorStore(
            _ => connectionString,
            _ => new SqlServerVectorStoreOptions
            {
                EmbeddingGenerator = embeddingGenerator,
            });
    })
    .Build();

var vectorStore = host.Services.GetRequiredService<SqlServerVectorStore>();

// Retrieval Helper
async Task<(string groundingText, IReadOnlyList<RetrievedChunk> chunks)> RetrieveRelevantChunksAsync(string query, CancellationToken ct = default)
{
    // 1. Embedded the query
    var queryEmbedding = await embeddingGenerator.GenerateVectorAsync(query,null,ct);
        
    // 2. Search the vector Store
    var collection = vectorStore.GetCollection<Guid, DocumentChunk>("chunks");

    // 3. Retrieve top-k relevant chunks with a minimum relevance threshold
    var results = await collection.SearchAsync(queryEmbedding, TopK).ToListAsync(ct);

    // Filter by minimum relevance
    var relevantChunks = results.Where(r=>r.Score >=MinRelevance)
        .OrderByDescending(r => r.Score)
        .ToList();

    if(relevantChunks.Count  == 0)
    {
        return ("No relevant context found in the documents store.", []);
    }

    var sb = new StringBuilder();
    sb.AppendLine("## Retreived Document Excerpts:");
    sb.AppendLine();

    var chunks = new List<RetrievedChunk>();
    foreach (var (item, idx) in relevantChunks.Select((x, i) => (x, i + 1)))
    {
        sb.AppendLine($"### Excerpt {idx}  (source: {item.Record.DocumentId}, relevance: {item.Score:F2})");
        sb.AppendLine(item.Record.Content);
        sb.AppendLine();
        chunks.Add(new RetrievedChunk(item.Record.DocumentId, item.Record.Content, item.Score ?? 0f));
    }

    return (sb.ToString(), chunks);
}

// ── System prompt factory ─────────────────────────────────────────────────────

static ChatMessage BuildSystemMessage(string groundingText) =>
    new(ChatRole.System,
        $"""
        You are a knowledgeable assistant that answers questions strictly based on
        the document excerpts provided below. Follow these rules:
 
        1. Answer only from the excerpts — do not use outside knowledge.
        2. If the excerpts do not contain enough information, say so clearly.
        3. Cite the source filename when you use a specific excerpt.
        4. Be concise and factual. Use bullet points for lists.
        5. If asked something unrelated to the documents, politely redirect.
 
        {groundingText}
        """);

// ── Conversation loop ─────────────────────────────────────────────────────────

var history = new List<ChatMessage>(MaxHistoryTurns * 2 + 1);

Console.Clear();
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════╗");
Console.WriteLine("║     Chat with your ingested documents    ║");
Console.WriteLine("╠══════════════════════════════════════════╣");
Console.WriteLine("║  Commands:  /clear  /sources  /quit      ║");
Console.WriteLine("╚══════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

IReadOnlyList<RetrievedChunk> lastChunks = [];

while (true)
{
    // User input
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("You › ");
    Console.ResetColor();
    string? input = Console.ReadLine()?.Trim();

    if(string.IsNullOrWhiteSpace(input)) continue;

    // ── Built-in commands ──────────────────────────────────────────────────
    if (input.Equals("/quit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Goodbye!");
        break;
    }

    if (input.Equals("/clear", StringComparison.OrdinalIgnoreCase))
    {
        history.Clear();
        lastChunks = [];
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("[Conversation history cleared]");
        Console.ResetColor();
        continue;
    }

    if (input.Equals("/sources", StringComparison.OrdinalIgnoreCase))
    {
        if (lastChunks.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("[No sources from last query]");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("\n── Sources used in last answer ──");
            foreach (var (chunk, i) in lastChunks.Select((c, i) => (c, i + 1)))
                Console.WriteLine($"  {i}. {chunk.SourceFile}  (score: {chunk.Score:F2})");
            Console.WriteLine();
            Console.ResetColor();
        }
        continue;
    }

    // ── Retrieve relevant chunks ───────────────────────────────────────────
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write("[Searching document store…] ");
    Console.ResetColor();

    var (groundingText, chunks) = await RetrieveRelevantChunksAsync(input);
    lastChunks = chunks;

    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"found {chunks.Count} relevant excerpt(s).");
    Console.ResetColor();

    // ── Build messages for this turn ───────────────────────────────────────
    // Re-inject the system message with fresh context each turn so the model
    // always has the most relevant excerpts for the CURRENT question.
    var messages = new List<ChatMessage>
    {
        BuildSystemMessage(groundingText),
    };

    // Append trimmed conversation history (keeps context without token bloat)
    int historyStart = Math.Max(0, history.Count - MaxHistoryTurns * 2);
    messages.AddRange(history.Skip(historyStart));

    // Add current user question
    messages.Add(new ChatMessage(ChatRole.User, input));

    // ── Call the LLM with streaming ────────────────────────────────────────
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("\nAssistant › ");
    Console.ResetColor();

    var answerBuilder = new StringBuilder();

    await foreach (var delta in chatClient.GetStreamingResponseAsync(messages))
    {
        string text = delta.Text ?? string.Empty;
        Console.Write(text);
        answerBuilder.Append(text);
    }

    Console.WriteLine("\n");

    // ── Update history ─────────────────────────────────────────────────────
    history.Add(new ChatMessage(ChatRole.User, input));
    history.Add(new ChatMessage(ChatRole.Assistant, answerBuilder.ToString()));
}


/// <summary>Lightweight DTO returned by <c>RetrieveAsync</c>.</summary>
public sealed record RetrievedChunk(string SourceFile, string Content, double Score);