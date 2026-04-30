using Azure.AI.OpenAI;
using ClassbasedSkills;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using Shared;

#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var skill = new UnitConverterSkill();
var skillProvider = new AgentSkillsProvider(skill);

AzureOpenAIClient client = Utils.GetAzureOpenAIClient(showRawCall: true);

AIAgent agent = client.GetChatClient("gpt-4o-mini").AsAIAgent(
    new ChatClientAgentOptions
    {
        Description = "Deals with unit converter",
        Name = "unit-converter-agent",
        ChatOptions = new ChatOptions()
        {
            Instructions = "Your tasks is to answer queries about the unit conversion"
        },
        AIContextProviders = [skillProvider],

    }).AsBuilder().Use(Utils.ToolCallingMiddleware).Build();

AgentResponse response = await agent.RunAsync("Can you convert 5 miles to kilometers?");
Console.WriteLine(response);
#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
