using AgentWithWebSearch;
using AgentWithWebSearch.Extensions;
using Google.GenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

Utils.Init("Google Gemini (WebSearch)");
Client client = new(apiKey: LLMConfig.GoogleGeminiApiKey);
IChatClient iChatClient = client.AsIChatClient("gemini-3-flash-preview");

string question = "What is today's Space news? ( Show today's date + Answer in max 20 words + a link)";

Utils.Green("No Web Search Tool");
ChatClientAgent normalAgent = new ChatClientAgent(iChatClient);
AgentResponse response1 = await normalAgent.RunAsync(question);
Console.WriteLine(response1);
response1.Usage.OutputAsInformation();

Utils.Separator();

Utils.Green("Web Search Tool (Easy)");
ChatClientAgent webSearchAgent = new(iChatClient, tools: [new HostedWebSearchTool() ]);
AgentResponse response2 = await webSearchAgent.RunAsync(question);
Console.WriteLine(response2);
response2.Usage.OutputAsInformation();

Utils.Separator();
