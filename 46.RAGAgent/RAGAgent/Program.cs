using Microsoft.Agents.AI;
using OpenAI;
using RAGAgent;
using RAGAgent.Agents;

Console.WriteLine("Hello, World!");

OpenAIClient openAIClient = new(LLMConfig.ApiKey);

var bookAgent = new BookAgent(openAIClient, LLMConfig.EmbeddingModel);

var agent = await bookAgent.CreateAgentAsync(openAIClient, LLMConfig.ChatModel);
var session = await agent.CreateSessionAsync();

// ✅ Create CostCenter — picks up pricing for gpt-4o-mini automatically
var costCenter = new CostCenter(bookAgent.ChatModel);

Console.WriteLine("📚 Book Advisor Agent");
Console.WriteLine("Ask me about reading tips, book genres, or book summaries.");
Console.WriteLine("Type 'exit' to quit.\n");

// Seed questions to demo the RAG retrieval
var demoQuestions = new[]
{
    "How can I read faster without losing comprehension?",
    "I want to start reading sci-fi. Where should I begin?",
    "What are the main ideas in Atomic Habits?",
    "Give me tips on building a daily reading habit.",
    "What's the difference between deep work and shallow work?",
};

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("💡 Try asking:");
foreach (var q in demoQuestions)
    Console.WriteLine($"   • {q}");
Console.ResetColor();
Console.WriteLine();

while (true)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("You: ");
    string? input = Console.ReadLine();
    if(string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
 
    AgentResponse response = await agent.RunAsync(input, session);
    // ✅ Record token usage from this turn
    costCenter.Record(response.Usage);


    // ✅ Show per-turn token cost inline
    if (response.Usage is not null)
    {
        long inTok = response.Usage.InputTokenCount ?? 0;
        long outTok = response.Usage.OutputTokenCount ?? 0;
        decimal turnCost = costCenter.CalculateCost(inTok, outTok);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[Tokens — in: {inTok:N0}  out: {outTok:N0}  cost: ${turnCost:F6}]");
        Console.ResetColor();
    }
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("Agent: ");    
    Console.WriteLine(response.Text);
    Console.ResetColor();
    Console.WriteLine();
}
costCenter.PrintSummary();

Console.WriteLine("Happy reading! 📖");