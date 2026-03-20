using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using OpenAI.Responses;
using Shared;

#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
// -------------------------------------------------------
// Step 1: Create the FileAgentSkillsProvider
//         It scans the /skills directory and discovers
//         all SKILL.md files (up to 2 levels deep)
// -------------------------------------------------------
var skillsProvider = new FileAgentSkillsProvider(
    skillPaths: [
        Path.Combine(AppContext.BaseDirectory, "skills")
        // You can add more skill directories here, e.g.:
        // Path.Combine(AppContext.BaseDirectory, "company-wide-skills")
    ]
);

AzureOpenAIClient client = Utils.GetAzureOpenAIClient(showRawCall: true);

# pragma warning disable OPENAI001
AIAgent helpDeskAgent = client.GetResponsesClient("gpt-4o-mini")
                            .AsAIAgent(new ChatClientAgentOptions
                            {
                                Name = "ITHelDeskAgent",
                                ChatOptions = new()
                                {
                                    Instructions = """
                                    You are the Contoso IT HelpDesk assistant.
                                    You help employees with IT-related questions.
                                    Use the available skills to provide accurate, 
                                    step-by-step guidance. Always be concise and friendly.
                                    """,
                                },
                                AIContextProviders = [skillsProvider]
                            });

// -------------------------------------------------------
// Step 3: Simulate employee queries
//         The agent auto-selects the right skill
// -------------------------------------------------------
var queries = new[]
{
    "Hi, I've been locked out of my account — wrong password too many times.",
    "My VPN keeps dropping on macOS, I've already tried restarting.",
    "I need to install Postman for API testing, how do I request it?"
};
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

AgentSession session = await helpDeskAgent.CreateSessionAsync();

Console.WriteLine("\n💬 IT HelpDesk ready! Type your question or 'exit' to quit.\n");

//foreach (var query in queries)
while (true)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write($"\n🧑 Employee: ");
    Console.ResetColor();

    var input = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(input)) continue;

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("🤖 HelpDesk: ");
    Console.ResetColor();

    // Straming for a better UX
    await foreach(var chunk in helpDeskAgent.RunStreamingAsync(input, session))
    {
        Console.Write(chunk);
    }
    Console.WriteLine("\n" + new string('-', 60));
}
Console.WriteLine("\n 👋 HelpDesk session ended. Goodbye!");