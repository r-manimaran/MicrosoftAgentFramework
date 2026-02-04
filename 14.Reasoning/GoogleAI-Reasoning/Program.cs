
// All modern models (2.5 and higher) can do thiniking

using GoogleAI_Reasoning;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Google.GenAI.Types;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using AgentFrameworkToolkit.Google;
using AgentFrameworkToolkit;

string apiKey = LLMConfig.ApiKey;
string question = "What is the capital city of Tamilnadu and how many people live there? (Answer back in max 3 words)";

Console.Clear();
Utils.WriteLineSuccess("Baseline using gemini 2.5 flash (Auto Thinking)");
await Baseline25();
Utils.Separator();

Utils.WriteLineSuccess("Baseline using gemini 3.5 flash preview (Auto Thinking");
await Baseline3();
Utils.Separator();

Utils.WriteLineSuccess("Raw: Geminin 2.5 (Thinking Budget =2000");
await Raw25();
Utils.Separator();

Utils.WriteLineSuccess("Raw: Gemini 3 Thinking Level = High");
await Raw3();
Utils.Separator();

Utils.WriteLineSuccess("Agent Framework Toolkit: Gemini 2.5 (Thinking Budget = 2000");
await AgentFrameworkToolkit25();
Utils.Separator();

Utils.WriteLineSuccess("Agent Framework Toolkit: Gemini 3(Thinking Level=High");
await AgentFrameworkToolkit3();
Utils.Separator();



/// Baseline 2.5 Model
async Task Baseline25()
{
    Google.GenAI.Client client = new(apiKey: apiKey);
    ChatClientAgent agent = new ChatClientAgent(client.AsIChatClient("gemini-2.5-flash"));
    AgentResponse response = await agent.RunAsync(question);
    Console.WriteLine(response);
    response.Usage.OutputAsInformation();
}

async Task Baseline3()
{
    Google.GenAI.Client client = new(apiKey: apiKey);
    ChatClientAgent agent = new ChatClientAgent(client.AsIChatClient("gemini-3-flash-preview"));
    AgentResponse response = await agent.RunAsync(question);
    Console.WriteLine(response);
    response.Usage.OutputAsInformation();
}

async Task Raw25()
{
    Google.GenAI.Client client = new(apiKey: apiKey);
    ChatClientAgent agent = new ChatClientAgent(client.AsIChatClient("gemini-2.5-flash"), new ChatClientAgentOptions
    {
        ChatOptions = new ChatOptions
        {
            RawRepresentationFactory = _ => new GenerateContentConfig
            {
                ThinkingConfig = new ThinkingConfig
                {
                    ThinkingBudget = 2000, // Max number of tokens to use (-1 = Auto, 0=Off)
                    // ThinkingLevel not supported (Setting both will leads to exception)
                    IncludeThoughts = true,
                }
            }
        }
    });

    AgentResponse response = await agent.RunAsync(question); Console.WriteLine(response);
    foreach(ChatMessage message in response.Messages)
    {
        foreach(AIContent content in message.Contents)
        {
            if(content is TextReasoningContent textReasoningContent)
            {
                Utils.WriteLineWarning("Reasoning Text");
                Utils.WriteLineInformation(textReasoningContent.Text);
            }
        }
    }
    response.Usage.OutputAsInformation();
}

async Task Raw3()
{
    Google.GenAI.Client client = new(apiKey: apiKey);
    ChatClientAgent agent = new ChatClientAgent(client.AsIChatClient("gemini-3-flash-preview"), new ChatClientAgentOptions
    {
        ChatOptions = new ChatOptions
        {
            RawRepresentationFactory = _ => new GenerateContentConfig
            {
                ThinkingConfig = new ThinkingConfig
                {
                    ThinkingLevel = ThinkingLevel.HIGH, // Pro can set High/LOW- Flash can set HIGH/MEDIUM/LOW/MINIMAL              
                    IncludeThoughts = true,
                }
            }
        }
    });

    AgentResponse response = await agent.RunAsync(question); Console.WriteLine(response);
    foreach (ChatMessage message in response.Messages)
    {
        foreach (AIContent content in message.Contents)
        {
            if (content is TextReasoningContent textReasoningContent)
            {
                Utils.WriteLineWarning("Reasoning Text");
                Utils.WriteLineInformation(textReasoningContent.Text);
            }
        }
    }
    response.Usage.OutputAsInformation();
}

async Task AgentFrameworkToolkit25()
{
    GoogleAgentFactory agentFactory = new GoogleAgentFactory(apiKey: apiKey);
    GoogleAgent agent = agentFactory.CreateAgent(new GoogleAgentOptions
    {
        Model = GoogleChatModels.Gemini25Flash,// Max number of tokens to use (-1 = Auto, 0=Off)
                                               // ThinkingLevel not supported (Setting both will leads to exception)
        ThinkingBudget = 2000
    });
    AgentResponse response = await agent.RunAsync(question);
    Console.WriteLine(response);
    TextReasoningContent? textReasoningContent = response.GetTextReasoningContent();
    if (textReasoningContent != null) {
        Utils.WriteLineWarning("Reasoning Text");
        Utils.WriteLineWarning(textReasoningContent.Text);
    }
    response.Usage.OutputAsInformation();
}

async Task AgentFrameworkToolkit3()
{
    GoogleAgentFactory agentFactory = new GoogleAgentFactory(apiKey: apiKey);
    GoogleAgent agent = agentFactory.CreateAgent(new GoogleAgentOptions
    {
        Model = "gemini-3-flash-preview",// Max number of tokens to use (-1 = Auto, 0=Off)
                                               // ThinkingLevel not supported (Setting both will leads to exception)
        ThinkingLevel = ThinkingLevel.HIGH
    });
    AgentResponse response = await agent.RunAsync(question);
    Console.WriteLine(response);
    TextReasoningContent? textReasoningContent = response.GetTextReasoningContent();
    if (textReasoningContent != null)
    {
        Utils.WriteLineWarning("Reasoning Text");
        Utils.WriteLineWarning(textReasoningContent.Text);
    }
    response.Usage.OutputAsInformation();
}