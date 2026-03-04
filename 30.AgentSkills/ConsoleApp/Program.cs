
using Azure.AI.OpenAI;
using ConsoleApp;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using Shared;

AzureOpenAIClient client = Utils.GetAzureOpenAIClient(showRawCall: true);

string skillsPath = "TestData\\AgentSkills";

#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
AIAgent agent = client.GetChatClient("gpt-4o-mini").AsAIAgent(new ChatClientAgentOptions
{
    AIContextProviders = [new FileAgentSkillsProvider(skillsPath)],
    ChatOptions = new Microsoft.Extensions.AI.ChatOptions
    {
        Tools = [AIFunctionFactory.Create(PythonRunner.RunPythonScript, name:"execute_python")]
    }
}).AsBuilder().Use(Utils.ToolCallingMiddleware).Build();
#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

await Utils.RunChatLoopWithSession(agent);
